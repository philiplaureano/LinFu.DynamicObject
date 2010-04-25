namespace LinFu.Reflection
{
    public interface IMixinAware
    {
        DynamicObject Self { get; set; }
    }
}