// Lucas Wilman da Silva Crispim
// Tradução para Português do Brasil (pt-BR) do jQuery Validation Plugin
// Sobrescreve os métodos de validação de data/número e define as mensagens em pt-BR.

jQuery.extend(jQuery.validator.methods, {
    date: function (value, element) {
        return this.optional(element) || /^\d\d?\/\d\d?\/\d\d\d?\d?$/.test(value);
    },
    number: function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(value);
    }
});

// Lucas Wilman da Silva Crispim
jQuery.extend(jQuery.validator.messages, {
    required: "Este campo é obrigatório.",
    remote: "Por favor, corrija este campo.",
    email: "Por favor, informe um endereço de e-mail válido.",
    url: "Por favor, informe uma URL válida.",
    date: "Por favor, informe uma data válida.",
    dateISO: "Por favor, informe uma data válida (ISO).",
    number: "Por favor, informe um número válido.",
    digits: "Por favor, informe somente dígitos.",
    creditcard: "Por favor, informe um número de cartão de crédito válido.",
    equalTo: "Por favor, informe o mesmo valor novamente.",
    extension: "Por favor, informe um valor com uma extensão válida.",
    maxlength: jQuery.validator.format("Por favor, informe no máximo {0} caracteres."),
    minlength: jQuery.validator.format("Por favor, informe pelo menos {0} caracteres."),
    rangelength: jQuery.validator.format("Por favor, informe um valor entre {0} e {1} caracteres."),
    range: jQuery.validator.format("Por favor, informe um valor entre {0} e {1}."),
    max: jQuery.validator.format("Por favor, informe um valor menor ou igual a {0}."),
    min: jQuery.validator.format("Por favor, informe um valor maior ou igual a {0}."),
    step: jQuery.validator.format("Por favor, informe um valor múltiplo de {0}.")
});
