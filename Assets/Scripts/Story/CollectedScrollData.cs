[System.Serializable]
public class CollectedScrollData
{
    public string id;
    public string title;
    public string body;

    public CollectedScrollData(string id, string title, string body)
    {
        this.id = id;
        this.title = title;
        this.body = body;
    }
}