using System;
using System.Globalization;
using System.Threading;
using AutomatedPublicTransportPlanning.Planning;
using UnityEngine;

// Signature: OpenAI-Codex_Automated_Public_Transport_Planning
public static class RouteStopPreviewChecks
{
    public static int Assertions;

    public static void Run()
    {
        Assertions = 0;
        Require(RouteCreationRules.GetStopSiteRejectionReason(null) == "No captured stop list.",
            "missing captured list has a concrete refusal reason");
        Require(RouteCreationRules.GetStopSiteRejectionReason(new RouteStopSite[0]).Contains("3-6"),
            "count refusal explains the supported stop range");
        float[] invalid = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        foreach (float value in invalid)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 position = new Vector3(axis == 0 ? value : 0f,
                    axis == 1 ? value : 0f, axis == 2 ? value : 0f);
                RouteStopSite[] sites = Sites(position);
                Require(!RouteCreationRules.HasValidStopSites(sites),
                    "non-finite stop coordinate must be rejected on axis " + axis);
                Require(RouteCreationRules.GetStopSiteRejectionReason(sites).Contains("Stop 1") &&
                    RouteCreationRules.GetStopSiteRejectionReason(sites).Contains("non-finite"),
                    "coordinate refusal identifies the one-based offending stop");
            }
        }
        Require(RouteCreationRules.HasValidStopSites(Sites(new Vector3(0f, -5f, 0f))),
            "finite negative elevation remains eligible; no new asset policy");
        Require(RouteCreationRules.GetStopSiteRejectionReason(Sites(new Vector3(0f, -5f, 0f))) == "",
            "valid copied data has no rejection reason");
        RouteStopSite[] sharedSegment = Sites(new Vector3(0f, 0f, 0f));
        sharedSegment[1] = new RouteStopSite(10, 7, false, 3, new Vector3(10f, 0f, 0f), true);
        Require(RouteCreationRules.HasValidStopSites(sharedSegment),
            "distinct separated lanes on one segment remain eligible at the pure-data layer");
        sharedSegment[1] = new RouteStopSite(10, 7, false, 2, new Vector3(10f, 0f, 0f), true);
        Require(!RouteCreationRules.HasValidStopSites(sharedSegment),
            "same segment/lane remains a duplicate regardless of direction");
        Require(RouteCreationRules.GetStopSiteRejectionReason(sharedSegment).Contains("Stops 1 and 2") &&
            RouteCreationRules.GetStopSiteRejectionReason(sharedSegment).Contains("same captured segment/lane"),
            "duplicate reason identifies both ordered stops");
        RouteStopSite[] threshold = Sites(new Vector3(7.5f, 0f, 0f));
        Require(!RouteCreationRules.HasValidStopSites(threshold), "existing 2.5 metre spacing boundary stays excluded");
        Require(RouteCreationRules.GetStopSiteRejectionReason(threshold).Contains("2.5 metres"),
            "spacing refusal explains the existing threshold");
        RouteStopSite[] missingLane = Sites(new Vector3(0f, 0f, 0f));
        missingLane[1] = new RouteStopSite(20, 8, false, -1, new Vector3(10f, 0f, 0f), true);
        Require(RouteCreationRules.GetStopSiteRejectionReason(missingLane).Contains("Stop 2") &&
            RouteCreationRules.GetStopSiteRejectionReason(missingLane).Contains("lane"),
            "missing lane refusal identifies the correct one-based stop");
        threshold[0] = new RouteStopSite(10, 7, true, 2, new Vector3(7.49f, 0f, 0f), true);
        Require(RouteCreationRules.HasValidStopSites(threshold), "finite sites just beyond existing spacing remain eligible");
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            string first = RouteStopPreview.Describe(10, 7, true, 2);
            Require(first == "10+ [lane 2; build 7]", "preview exposes exact captured lane/build invariantly");
            Require(first != RouteStopPreview.Describe(10, 7, true, 3), "same segment different lane is distinguishable");
            Require(first != RouteStopPreview.Describe(10, 8, true, 2), "replaced segment build is distinguishable");
            Require(first != RouteStopPreview.Describe(10, 7, false, 2), "direction is distinguishable");
        }
        finally { Thread.CurrentThread.CurrentCulture = original; }
    }

    private static RouteStopSite[] Sites(Vector3 first)
    {
        return new RouteStopSite[]
        {
            new RouteStopSite(10, 7, true, 2, first, true),
            new RouteStopSite(20, 8, false, 3, new Vector3(10f, 0f, 0f), true),
            new RouteStopSite(30, 9, true, 4, new Vector3(20f, 0f, 0f), false)
        };
    }

    private static void Require(bool condition, string message)
    {
        Assertions++;
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
