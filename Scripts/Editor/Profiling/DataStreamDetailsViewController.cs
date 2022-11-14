using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Entities.Tasks;
using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.UIElements;
using Type = System.Type;

namespace Anvil.Unity.DOTS.Editor.Profiling
{
    public class DataStreamDetailsViewController : ProfilerModuleViewController
    {
        private readonly Dictionary<Type, Label> m_TypeLabels;
        private VisualElement m_View;
        private ScrollView m_ScrollView;

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

            foreach (DataStreamProfilingUtil.AggCounterForType agg in DataStreamProfilingUtil.StatsByType.Values)
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
            string[] suf = new string[]{ "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
            {
                return "0" + suf[0];
            }
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
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
