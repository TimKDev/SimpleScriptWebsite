using System.Text.RegularExpressions;

namespace SimpleScriptWebSite.Services;

internal class InputValidator : IInputValidator
{
    public bool ValidateStartCommand(string startCommand)
    {
        if (string.IsNullOrWhiteSpace(startCommand))
        {
            return false;
        }

        var pattern = @"^execute-direct\s+""(.*?)""$";
        return Regex.IsMatch(startCommand, pattern, RegexOptions.Singleline);
    }

    public bool ValidateInput(string input)
    {
        return true;
    }
}