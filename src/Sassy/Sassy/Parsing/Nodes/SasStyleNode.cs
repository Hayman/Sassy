using System.Collections;
using System.Collections.Generic;

namespace Sassy.Parsing.Nodes
{
    public sealed class SasStyleNode
    {
        public SasStyleNode()
        {
            StyleProperties = new List<SasStyleProperty>();
        }

        public IList Invocations { get; }

        public IList<SasStyleProperty> StyleProperties { get; }

        public SasStyleSelector StyleSelector { get; }

        public SasDeviceSelector DeviceSelector { get; }

        public void AddStyleProperty(SasStyleProperty styleProperty)
        {
            StyleProperties.Add(styleProperty);
        }
    }
}