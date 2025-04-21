namespace SimpleScriptWebSite.Services;

internal interface IInputValidator
{
    bool ValidateStartCommand(string startCommand);
    bool ValidateInput(string input);
}