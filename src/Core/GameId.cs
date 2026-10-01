namespace Arietta.Core;

public class GameId
{
    public GameBiz GameBiz { get; set; }
    public string Id { get; set; } = string.Empty;

    public GameId(GameBiz gameBiz, string id)
    {
        GameBiz = gameBiz;
        Id = id;
    }
}
