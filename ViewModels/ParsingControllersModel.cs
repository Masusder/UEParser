using System;
using System.Windows.Input;
using ReactiveUI;
using UEParser.Views;

namespace UEParser.ViewModels
{
    public class ParsingControllersModel : ReactiveObject
    {
        private static readonly Lazy<ParsingControllersModel> lazy = new(() => new());
        public static ParsingControllersModel Instance => lazy.Value;

        public ICommand ParseEverythingCommand { get; }
        public ICommand ParseRiftsCommand { get; }
        public ICommand ParseCharactersCommand { get; }

        private ParsingControllersModel()
        {
            ParseEverythingCommand = ReactiveCommand.Create(ParseEverything);
            ParseRiftsCommand = ReactiveCommand.Create(ParseRifts);
            ParseCharactersCommand = ReactiveCommand.Create(ParseCharacters);
        }

        private void ParseEverything()
        {
            LogsWindowModel.Instance.AddLog("[INFO] Parsing all data..");
            LogsWindowModel.Instance.AddLog("[SUCCESS] Data parsed successfully.");
        }

        private void ParseRifts()
        {
            LogsWindowModel.Instance.AddLog("[INFO] [Rifts] Parsing data..");
            LogsWindowModel.Instance.AddLog("[SUCCESS] [Rifts] Data parsed successfully.");
        }

        private void ParseCharacters()
        {
            LogsWindowModel.Instance.AddLog("[INFO] [Characters] Parsing data..");
            LogsWindowModel.Instance.AddLog("[SUCCESS] [Characters] Data parsed successfully.");
        }
    }
}
