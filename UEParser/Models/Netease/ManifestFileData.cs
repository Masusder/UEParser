using System.ComponentModel;

namespace UEParser.Models.Netease
{
    public class ManifestFileData : INotifyPropertyChanged
    {
        public required string FilePath { get; set; }
        public required string FileExtension { get; set; }
        public required string FilePathWithExtension { get; set; }
        public required string FileHash { get; set; }
        public long FileSize { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
