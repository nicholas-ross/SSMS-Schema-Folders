
namespace SsmsSchemaFolders
{
    public interface IDebugOutput
    {
        void debug_message(string message);
        void debug_message(string message, params object[] args);
        //void ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE entryType, string message);

    }
}
