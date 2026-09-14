namespace PracticeLanguageWords.Application.Common;

/// <summary>Istenen kayit bulunamadi (Web katmani bunu 404'e cevirir).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Is kurali ihlali (Web katmani bunu kullaniciya gosterilebilir hataya cevirir).</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
