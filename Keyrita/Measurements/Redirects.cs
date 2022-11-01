﻿using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Computes the rolls found in the keyboard layout.
    /// </summary>
    public class RedirectsResult : AnalysisResult
    {
        public RedirectsResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalRedirects { get; set; }
    }

    public class Redirects : DynamicMeasurement
    {
        private RedirectsResult mResult;

        public Redirects() : base(eMeasurements.Redirects)
        {
            mResult = new RedirectsResult(NodeId);
            AddInputNode(eInputNodes.TrigramStats);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            long totalTgs = SettingState.MeasurementSettings.CharFrequencyData.TrigramHitCount;
            TrigramStatsResult tgs = (TrigramStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TrigramStats];
            mResult.TotalRedirects = tgs.TotalRedirects / totalTgs * 100;

            SetResult(0, mResult.TotalRedirects);
        }
    }
}
