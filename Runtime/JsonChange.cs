#if NEWTONSOFT_EXISTS
using Newtonsoft.Json.Linq;

namespace DingoJsonUI
{
    public readonly struct JsonChange
    {
        public string Path { get; }
        public JToken PreviousValue { get; }
        public JToken CurrentValue { get; }

        public bool HasChanged => !JToken.DeepEquals(PreviousValue, CurrentValue);

        public JsonChange(string path, JToken previousValue, JToken currentValue)
        {
            Path = JsonPath.Normalize(path);
            PreviousValue = previousValue;
            CurrentValue = currentValue;
        }

        public override string ToString() => $"{Path}: {PreviousValue} -> {CurrentValue}";
    }
}
#endif
