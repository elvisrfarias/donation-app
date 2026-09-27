using ErrorOr;

namespace DoacaoApp.Domain.Errors;

/// <summary>
/// Catálogo central de erros de negócio. Usar <see cref="Error"/> (ErrorOr) em vez de
/// exceptions para o fluxo de domínio — exceptions ficam reservadas a falhas inesperadas.
/// Cada erro carrega um código estável (consumível por clientes) e uma descrição.
/// </summary>
public static class DomainErrors
{
    public static class Cpf
    {
        public static Error Empty => Error.Validation("Cpf.Empty", "O CPF é obrigatório.");
        public static Error InvalidFormat => Error.Validation("Cpf.InvalidFormat", "O CPF deve conter 11 dígitos.");
        public static Error InvalidCheckDigits => Error.Validation("Cpf.InvalidCheckDigits", "Os dígitos verificadores do CPF são inválidos.");
    }

    public static class Email
    {
        public static Error Empty => Error.Validation("Email.Empty", "O e-mail é obrigatório.");
        public static Error InvalidFormat => Error.Validation("Email.InvalidFormat", "O e-mail informado é inválido.");
    }

    public static class PhoneNumber
    {
        public static Error Empty => Error.Validation("PhoneNumber.Empty", "O telefone é obrigatório.");
        public static Error InvalidFormat => Error.Validation("PhoneNumber.InvalidFormat", "O telefone deve ter DDD + número (10 ou 11 dígitos).");
    }

    public static class Address
    {
        public static Error Incomplete => Error.Validation("Address.Incomplete", "Endereço incompleto: logradouro, cidade, estado e CEP são obrigatórios.");
        public static Error InvalidZipCode => Error.Validation("Address.InvalidZipCode", "O CEP deve conter 8 dígitos.");
        public static Error InvalidState => Error.Validation("Address.InvalidState", "A UF deve ter 2 letras.");
        public static Error InvalidCoordinates => Error.Validation("Address.InvalidCoordinates", "As coordenadas informadas estão fora dos limites válidos (latitude -90..90, longitude -180..180).");
    }

    public static class WithdrawalCycle
    {
        public static Error InvalidMax => Error.Validation("WithdrawalCycle.InvalidMax", "O limite de retiradas por ciclo deve ser maior que zero.");
    }

    public static class CollectionPoint
    {
        public static Error NameRequired => Error.Validation("CollectionPoint.NameRequired", "O nome do ponto de coleta é obrigatório.");
        public static Error Inactive => Error.Conflict("CollectionPoint.Inactive", "O ponto de coleta está inativo e não pode registrar retiradas.");
        public static Error NotFound => Error.NotFound("CollectionPoint.NotFound", "Ponto de coleta não encontrado.");

        public static Error StockItemNameRequired =>
            Error.Validation("CollectionPoint.StockItemNameRequired", "O nome do item de estoque é obrigatório.");

        public static Error StockItemUnitRequired =>
            Error.Validation("CollectionPoint.StockItemUnitRequired", "A unidade do item de estoque é obrigatória.");

        public static Error NegativeQuantity =>
            Error.Validation("CollectionPoint.NegativeQuantity", "A quantidade não pode ser negativa.");

        public static Error DuplicateStockItemName =>
            Error.Conflict("CollectionPoint.DuplicateStockItemName", "Já existe um item de estoque com este nome neste ponto.");

        public static Error StockItemNotFound =>
            Error.NotFound("CollectionPoint.StockItemNotFound", "Item de estoque não encontrado neste ponto.");

        public static Error StockItemInUseByBasketTemplate =>
            Error.Conflict(
                "CollectionPoint.StockItemInUseByBasketTemplate",
                "Este item está em uso em uma receita de cesta e não pode ser removido.");

        public static Error BasketTemplateNotFound =>
            Error.NotFound("CollectionPoint.BasketTemplateNotFound", "Receita de cesta não encontrada neste ponto.");

        public static Error BasketTemplateNameRequired =>
            Error.Validation("CollectionPoint.BasketTemplateNameRequired", "O nome da receita de cesta é obrigatório.");

        public static Error BasketTemplateItemsRequired =>
            Error.Validation("CollectionPoint.BasketTemplateItemsRequired", "Informe ao menos um item na receita de cesta.");

        public static Error BasketTemplateDuplicateItem =>
            Error.Validation(
                "CollectionPoint.BasketTemplateDuplicateItem",
                "Um item não pode aparecer mais de uma vez na mesma receita de cesta.");

        public static Error BasketTemplateInvalidQuantity =>
            Error.Validation(
                "CollectionPoint.BasketTemplateInvalidQuantity",
                "A quantidade de cada item da receita deve ser maior que zero.");

        public static Error BasketTemplateInUseByWithdrawal =>
            Error.Conflict(
                "CollectionPoint.BasketTemplateInUseByWithdrawal",
                "Esta receita já foi usada em alguma retirada e não pode ser removida. Desative-a em vez de remover.");
    }

    public static class StockSetting
    {
        public static Error NegativeValue =>
            Error.Validation("StockSetting.NegativeValue", "A meta e o aviso de estoque não podem ser negativos.");

        public static Error AlertAboveGoal =>
            Error.Validation("StockSetting.AlertAboveGoal", "O aviso de estoque baixo não pode ser maior que a meta.");
    }

    public static class Beneficiary
    {
        public static Error NameRequired => Error.Validation("Beneficiary.NameRequired", "O nome do beneficiário é obrigatório.");
        public static Error NotFound => Error.NotFound("Beneficiary.NotFound", "Beneficiário não encontrado.");
        public static Error CpfAlreadyRegistered => Error.Conflict("Beneficiary.CpfAlreadyRegistered", "Este CPF já está cadastrado no município.");

        public static Error BirthDateRequired =>
            Error.Validation("Beneficiary.BirthDateRequired", "A data de nascimento é obrigatória.");

        public static Error PhoneRequired =>
            Error.Validation("Beneficiary.PhoneRequired", "Informe ao menos um telefone de contato.");

        public static Error TooManyPhones =>
            Error.Validation("Beneficiary.TooManyPhones", "Informe no máximo 2 telefones de contato.");

        public static Error AlreadyLinkedToPoint =>
            Error.Conflict("Beneficiary.AlreadyLinkedToPoint", "Este beneficiário já está vinculado a este ponto.");

        public static Error RelatedBeneficiaryNotFound =>
            Error.NotFound("Beneficiary.RelatedBeneficiaryNotFound", "Não foi encontrado nenhum beneficiário com o CPF informado.");

        public static Error SelfKinshipNotAllowed =>
            Error.Validation("Beneficiary.SelfKinshipNotAllowed", "Um beneficiário não pode ter um vínculo de parentesco consigo mesmo.");

        public static Error KinshipAlreadyExists =>
            Error.Conflict("Beneficiary.KinshipAlreadyExists", "Já existe um vínculo de parentesco registrado entre esses dois beneficiários.");
    }

    public static class StoredFile
    {
        public static Error KeyRequired => Error.Validation("StoredFile.KeyRequired", "O bucket e a chave do objeto são obrigatórios.");
        public static Error NameRequired => Error.Validation("StoredFile.NameRequired", "O nome do arquivo é obrigatório.");
        public static Error ContentTypeRequired => Error.Validation("StoredFile.ContentTypeRequired", "O tipo do arquivo (content-type) é obrigatório.");
        public static Error Empty => Error.Validation("StoredFile.Empty", "O arquivo está vazio.");
        public static Error NotFound => Error.NotFound("StoredFile.NotFound", "Arquivo não encontrado.");

        public static Error UnsupportedContentType(string contentType) =>
            Error.Validation("StoredFile.UnsupportedContentType", $"Tipo de arquivo não suportado: {contentType}.");

        public static Error TooLarge(int maxMegabytes) =>
            Error.Validation("StoredFile.TooLarge", $"O arquivo excede o tamanho máximo de {maxMegabytes} MB.");

        public static Error UploadFailed => Error.Failure("StoredFile.UploadFailed", "Falha ao enviar o arquivo para o armazenamento.");
    }

    public static class Review
    {
        public static Error AuthorRequired => Error.Validation("Review.AuthorRequired", "Informe seu nome para avaliar o ponto de coleta.");

        public static Error InvalidRating(int min, int max) =>
            Error.Validation("Review.InvalidRating", $"A nota deve ser um número entre {min} e {max}.");

        public static Error CommentTooLong(int max) =>
            Error.Validation("Review.CommentTooLong", $"O comentário excede o limite de {max} caracteres.");

        public static Error NotFound => Error.NotFound("Review.NotFound", "Avaliação não encontrada.");
    }

    public static class Withdrawal
    {
        public static Error CategoriesRequired =>
            Error.Validation("Withdrawal.CategoriesRequired", "Informe ao menos um item da retirada.");

        public static Error ItemUnavailable =>
            Error.Conflict("Withdrawal.ItemUnavailable", "Um dos itens selecionados não está disponível no estoque deste ponto.");

        public static Error CycleLimitReached(int max) =>
            Error.Conflict(
                "Withdrawal.CycleLimitReached",
                $"O beneficiário já atingiu o limite de {max} retirada(s) no ciclo vigente (considerando todos os pontos do município).");

        public static Error NotFound => Error.NotFound("Withdrawal.NotFound", "Retirada não encontrada.");

        public static Error OnlyMostRecentIsEditable =>
            Error.Conflict(
                "Withdrawal.OnlyMostRecentIsEditable",
                "Só é possível editar a retirada mais recente do beneficiário.");

        public static Error InsufficientStock =>
            Error.Conflict(
                "Withdrawal.InsufficientStock",
                "Não há estoque suficiente para montar a(s) cesta(s) selecionada(s).");

        public static Error BasketTemplateNotFound =>
            Error.NotFound("Withdrawal.BasketTemplateNotFound", "A receita de cesta selecionada não foi encontrada neste ponto.");

        public static Error BasketTemplateInactive =>
            Error.Conflict("Withdrawal.BasketTemplateInactive", "A receita de cesta selecionada está inativa.");
    }
}