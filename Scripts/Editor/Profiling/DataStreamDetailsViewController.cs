using Anvil.Unity.DOTS.Entities.Tasks;
using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace Anvil.Unity.DOTS.Editor.Profiling
{
    //TODO: #108 - Improved Profiler features
    /// <summary>
    /// Profiling details to be shown when selecting the <see cref="DataStreamProfilerModule"/> in Unity's Profiler
    /// </summary>
    public class DataStreamDetailsViewController : ProfilerModuleViewController
    {
        private readonly Dictionary<Type, Label> m_TypeLabels;
        private VisualElement m_View;
        private ScrollView m_ScrollView;

        /// <summary>
        /// Creates a new instance of the View Controller
        /// Called by <see cref="DataStreamProfilerModule"/>
        /// </summary>
        /// <param name="profilerWindow">Reference to the <see cref="ProfilerWindow"/> for this view controller to be
        /// displayed in.</param>
        public DataStreamDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow)
        {
            m_TypeLabels = new Dictionary<Type, Label>();
        }

        protected override VisualElement CreateView()
        {
            m_View = new VisualElement();

            m_ScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_View.Add(m_ScrollView);
            
            ProfilerWindow.SelectedFrameIndexChanged += ProfilerWindow_OnSelectedFrameIndexChanged;
            return m_View;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            ProfilerWindow.SelectedFrameIndexChanged -= ProfilerWindow_OnSelectedFrameIndexChanged;

            base.Dispose(true);
        }

        private void ProfilerWindow_OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            RawFrameDataView dataView = ProfilerDriver.GetRawFrameDataView((int)selectedFrameIndex, 0);

            foreach (DataStreamProfilingUtil.ProfilingInfoForDataStreamType agg in DataStreamProfilingUtil.StatsByType.Values)
            {
                Label label = GetOrCreateLabelByType(agg.Type);
                int typeLiveInstances = GetCount(agg.MNLiveInstances, dataView);
                int typeLiveCapacity = GetCount(agg.MNLiveCapacity, dataView);
                int typePendingCapacity = GetCount(agg.MNPendingCapacity, dataView);
                long typeLiveInstancesBytes = GetBytes(agg.MNLiveInstanceBytes, dataView);
                long typeLiveCapacityBytes = GetBytes(agg.MNLiveCapacityBytes, dataView);
                long typePendingCapacityBytes = GetBytes(agg.MNPendingCapacityBytes, dataView);
                

                label.text = $"{agg.ReadableTypeName} - {agg.ReadableInstanceTypeName} ({BytesToString(agg.BytesPerInstance)})\nLive: {BytesToString(typeLiveInstancesBytes)} ({typeLiveInstances:N0})\nCapacity: {BytesToString(typeLiveCapacityBytes)} ({typeLiveCapacity:N0})\nPending Capacity: {BytesToString(typePendingCapacityBytes)}  ({typePendingCapacity:N0})\nTotal Capacity: {BytesToString(typeLiveCapacityBytes + typePendingCapacityBytes)}  ({(typeLiveCapacity + typePendingCapacity):N0})";
            }
        }

        private string BytesToString(long byteCount)
        {
            string[] suffixes = new string[]{ "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
            {
                return $"0{suffixes[0]}";
            }
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{(Math.Sign(byteCount) * num)}{suffixes[place]}";
        }

        private int GetCount(string id, RawFrameDataView dataView)
        {
            int markerID = dataView.GetMarkerId(id);
            return dataView.GetCounterValueAsInt(markerID);
        }

        private long GetBytes(string id, RawFrameDataView dataView)
        {
            int markerID = dataView.GetMarkerId(id);
            return dataView.GetCounterValueAsLong(markerID);
        }

        private Label GetOrCreateLabelByType(Type type)
        {
            if (!m_TypeLabels.TryGetValue(type, out Label label))
            {
                label = new Label()
                {
                    style =
                    {
                        paddingTop = 8, paddingLeft = 8
                    }
                };
                m_ScrollView.Add(label);
                m_TypeLabels.Add(type, label);
            }

            return label;
        }
    }
}
