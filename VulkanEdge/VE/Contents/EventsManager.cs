namespace VE.Contents;

public class EventsManager
{
    public Dictionary<string,List<GameObject>> Queues { get; init; } = new();

    public void Register(string name, GameObject gameobject)
    {
        if (!Queues.ContainsKey(name)) Queues.Add(name, new());

        Queues[name].Add(gameobject);
    }

    public void Deregister(GameObject gameobject)
    {
        foreach (var k in Queues.Keys)
            Queues[k].Remove(gameobject);
    }

    public void Trigger(string name)
    {
        if (Queues.ContainsKey(name))
        {
            foreach (var gameObject in Queues[name])
            {
                gameObject.Trigger(name);
            }
        }
    }
}
