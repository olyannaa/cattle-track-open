using System.Threading.Channels;
using CAT.Controllers.DTO;

public class UserActionQueue
{
    private readonly Channel<UserActionDto> _channel;

    public UserActionQueue()
    {
        _channel = Channel.CreateUnbounded<UserActionDto>();
    }

    public void Enqueue(UserActionDto dto)
    {
        if (!_channel.Writer.TryWrite(dto))
        {
            throw new InvalidOperationException("Не удалось записать в очередь логов");
        }
    }

    public ChannelReader<UserActionDto> Reader => _channel.Reader;
}