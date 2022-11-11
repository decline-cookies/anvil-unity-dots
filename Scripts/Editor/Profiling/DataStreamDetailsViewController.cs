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

        public DataStreamDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow)
        {
            m_TypeLabels = new Dictionary<Type, Label>();
        }

        protected override VisualElement CreateView()
        {
            m_View = new VisualElement();
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

                label.text = $"{agg.ReadableTypeName} - Live: {typeLiveInstances}, Capacity: {typeLiveCapacity}, Pending Capacity: {typePendingCapacity}, Total Capacity: {typeLiveCapacity + typePendingCapacity}";
            }
        }

        private int GetCount(string id, RawFrameDataView dataView)
        {
            int markerID = dataView.GetMarkerId(id);
            return dataView.GetCounterValueAsInt(markerID);
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
                m_View.Add(label);
                m_TypeLabels.Add(type, label);
            }

            return label;
        }
    }
}
