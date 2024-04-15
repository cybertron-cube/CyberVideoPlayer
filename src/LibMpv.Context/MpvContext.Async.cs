namespace LibMpv.Context;

public partial class MpvContext
{
    private readonly MpvUserDataStorage _mpvCommandUserDataStorage = new();
    
    public async Task CommandAsync(params string[] args)
    {
        var tcs = new TaskCompletionSource<int>();
        var userData = _mpvCommandUserDataStorage.GetAvailableUserData();
        
        AsyncCommandReply += OnAsyncCommandReply;
        CommandAsync(userData, args);
        
        var code = await tcs.Task;
        
        AsyncCommandReply -= OnAsyncCommandReply;
        _mpvCommandUserDataStorage.FreeUserData(userData);
        
        CheckCode(code);
        
        return;
        
        void OnAsyncCommandReply(object? _, MpvReplyEventArgs e)
        {
            if (e.ReplyData == userData)
            {
                tcs.SetResult(e.ErrorCode);
            }
        }
    }
}