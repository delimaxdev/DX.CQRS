using System.Collections.Generic;
using System.Linq;

namespace DX.Cqrs.Codegen.Core
{
    internal class GenerationContext {
        private readonly SourceItem[] _allSourceItems;

        public GenerationContext(SourceItem[] sourceItems) {
            _allSourceItems = sourceItems;
        }

        public Maybe<SourceClass> GetSourceClass(string name) {
            return FindSourceClass(name, _allSourceItems).NoneIfNull();
        }

        private SourceClass FindSourceClass(string name, IEnumerable<SourceItem> items) {
            SourceClass result = items
                .OfType<SourceClass>()
                .FirstOrDefault(x => x.Name == name);

            if (result == null) {
                result = items
                    .OfType<SourceContainerType>()
                    .Select(c => FindSourceClass(name, c.Items))
                    .FirstOrDefault(r => r != null);
            }

            return result;
        }
    }
}
