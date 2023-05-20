namespace VE;

public class ContentManager
{
    // Content Loaded in the types it needs to be stored in 1 place as object - filename string points to object
    Dictionary<string, object> Content { get; } = new();
    // Content loaded in stores its type here for ease of interaction for the code - filename string points to object type
    Dictionary<string, string> Content_Type { get; } = new();


}
