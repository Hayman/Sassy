using Foundation;

namespace Sassy.Parsing
{
    public class SasInvocation
    {
        public NSInvocation Invocation { get; set; }

        public string KeyPath { get; set; }

        public SasInvocation(NSInvocation invocation, string keyPath)
        {
            Invocation = invocation;
            KeyPath = keyPath;
        }

        public void Invoke(NSObject target)
        {
            var resolvedTarget = KeyPath == null
                ? target
                : target.ValueForKeyPath(new NSString(KeyPath));

            Invocation.Invoke(resolvedTarget);
        }
    }
}