using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System.ComponentModel.DataAnnotations;

namespace HalalStocks.Helpers;

/// <summary>
/// Supplies Arabic text for validation attributes left without an explicit ErrorMessage.
/// Messages deliberately omit the field name: the view models carry no [Display] names, so
/// "{0}" would render the English property name inside an otherwise Arabic sentence.
/// </summary>
public class ArabicValidationMessagesProvider : IValidationMetadataProvider
{
    private const string RequiredMessage = "هذا الحقل مطلوب";

    private static readonly Dictionary<Type, string> Defaults = new()
    {
        [typeof(RequiredAttribute)]          = RequiredMessage,
        [typeof(EmailAddressAttribute)]      = "يرجى إدخال بريد إلكتروني صحيح",
        [typeof(PhoneAttribute)]             = "يرجى إدخال رقم هاتف صحيح",
        [typeof(UrlAttribute)]               = "يرجى إدخال رابط صحيح",
        [typeof(CompareAttribute)]           = "القيمتان غير متطابقتين",
        [typeof(RangeAttribute)]             = "القيمة المدخلة خارج النطاق المسموح",
        [typeof(StringLengthAttribute)]      = "طول النص المدخل غير مسموح",
        [typeof(MaxLengthAttribute)]         = "النص المدخل أطول من المسموح",
        [typeof(MinLengthAttribute)]         = "النص المدخل أقصر من المسموح",
        [typeof(RegularExpressionAttribute)] = "صيغة القيمة المدخلة غير صحيحة"
    };

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        var validators = context.ValidationMetadata.ValidatorMetadata;

        foreach (var attribute in validators.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessage is null &&
                attribute.ErrorMessageResourceName is null &&
                Defaults.TryGetValue(attribute.GetType(), out var message))
            {
                attribute.ErrorMessage = message;
            }
        }

        // Non-nullable value types (int, bool, enum, DateTime, ...) are implicitly required, but
        // the framework builds that RequiredAttribute inside the client-validator provider — too
        // late for the loop above to reach. Declaring one here pre-empts it with Arabic text.
        // RequiredAttribute only ever fails on null, so this changes the message, not the rules.
        if (context.Key.MetadataKind == ModelMetadataKind.Property &&
            context.Key.ModelType.IsValueType &&
            Nullable.GetUnderlyingType(context.Key.ModelType) is null &&
            !validators.OfType<RequiredAttribute>().Any())
        {
            validators.Add(new RequiredAttribute { ErrorMessage = RequiredMessage });
        }
    }
}
