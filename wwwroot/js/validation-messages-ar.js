// Arabic defaults for jQuery Validation's built-in rules.
// Rules derived from HTML5 attributes (min/max/step/number) carry no server-side
// ErrorMessage, so without this override they render in English.
(function ($) {
    if (!$ || !$.validator) { return; }

    $.extend($.validator.messages, {
        required:    "هذا الحقل مطلوب",
        remote:      "يرجى تصحيح هذه القيمة",
        email:       "يرجى إدخال بريد إلكتروني صحيح",
        url:         "يرجى إدخال رابط صحيح",
        date:        "يرجى إدخال تاريخ صحيح",
        dateISO:     "يرجى إدخال تاريخ صحيح",
        number:      "يرجى إدخال رقم صحيح",
        digits:      "يرجى إدخال أرقام فقط",
        equalTo:     "يرجى إدخال نفس القيمة مرة أخرى",
        maxlength:   $.validator.format("يرجى إدخال {0} حرفاً كحد أقصى"),
        minlength:   $.validator.format("يرجى إدخال {0} حرفاً على الأقل"),
        rangelength: $.validator.format("يرجى إدخال قيمة يتراوح طولها بين {0} و {1} حرفاً"),
        range:       $.validator.format("يرجى إدخال قيمة بين {0} و {1}"),
        max:         $.validator.format("يرجى إدخال قيمة أقل من أو تساوي {0}"),
        min:         $.validator.format("يرجى إدخال قيمة أكبر من أو تساوي {0}"),
        step:        $.validator.format("يرجى إدخال مضاعفات الرقم {0}")
    });
}(window.jQuery));
