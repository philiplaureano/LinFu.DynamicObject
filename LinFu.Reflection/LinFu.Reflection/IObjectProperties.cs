namespace LinFu.Reflection
{
    public interface IObjectProperties
    {
        object this[string propertyName] { get; set; }
    }
}