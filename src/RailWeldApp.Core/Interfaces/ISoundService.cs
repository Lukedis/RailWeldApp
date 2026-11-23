namespace RailWeldApp.Interfaces
{
    public interface ISoundService : IDisposable
    {
        void Play(string soundName = "Popup");

        void Preload(string soundName = "Popup");

        void Stop(string soundName = "Popup");
    }
}
