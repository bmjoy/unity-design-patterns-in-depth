namespace AdvancedSceneManager.Models
{

    /// <summary>Identifies either <see cref="SceneCollection"/> or <see cref="Scene"/>.</summary>
    public interface ISceneObject
    {

        string name { get; }
        void SetName(string name);
        void OnPropertyChanged();

    }

}
