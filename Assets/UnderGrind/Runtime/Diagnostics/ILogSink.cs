namespace UnderGrind.Diagnostics
{
    public interface ILogSink
    {
        void Write(in LogEvent entry);
    }
}
