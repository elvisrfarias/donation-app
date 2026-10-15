# Diagramas UML

## Casos de uso

```mermaid
flowchart LR
    V([Visitante])
    G([Gestor de ponto])
    A([Administrador])

    subgraph Público
        UC1(Consultar pontos no mapa)
        UC2(Avaliar ponto)
        UC3(Avisar que vai doar)
        UC4(Ler avisos do ponto)
    end
    subgraph Gestão do ponto
        UC5(Gerenciar estoque)
        UC6(Registrar retirada)
        UC7(Cadastrar beneficiário)
        UC8(Confirmar ou cancelar doação)
        UC9(Publicar aviso no mural)
        UC10(Ver atividade recente)
    end
    subgraph Administração
        UC11(Cadastrar ponto)
        UC12(Gerenciar usuários)
        UC13(Moderar avaliações)
    end

    V --- UC1 & UC2 & UC3 & UC4
    G --- UC5 & UC6 & UC7 & UC8 & UC9 & UC10
    A --- UC11 & UC12 & UC13
    A -. também faz o que o gestor faz .- G
```

## Modelo de domínio (agregados)

```mermaid
classDiagram
    class CollectionPoint {
        +Name
        +Address
        +ContactInfo
        +WithdrawalRule
        +RegisterWithdrawal()
        +ReceiveStock()
        +ReplaceStockQuantities()
    }
    class StockItem {
        +Name
        +Unit
        +Quantity
        +Goal
        +AlertThreshold
    }
    class Withdrawal
    class Beneficiary {
        +Cpf
        +Email
        +PointLinks
    }
    class Review {
        +Rating
        +Comment
        +Hide()
    }
    class Donation {
        +DonorName
        +DonorPhone
        +Description
        +Status
        +MarkReceived()
        +Cancel()
    }
    class PointNotice {
        +Title
        +Message
        +Audience
        +Archive()
    }

    CollectionPoint "1" *-- "many" StockItem
    CollectionPoint "1" *-- "many" Withdrawal
    Withdrawal ..> Beneficiary : por Id
    Review ..> CollectionPoint : por Id
    Donation ..> CollectionPoint : por Id
    PointNotice ..> CollectionPoint : por Id
```

Linhas cheias são composição dentro do agregado; linhas pontilhadas são referências por Id entre
agregados (não há navegação EF entre eles).

## Atividades: ciclo de uma doação

```mermaid
flowchart TD
    A([Doador abre o ponto no mapa]) --> B[Preenche nome e o que vai doar]
    B --> C{Ponto ativo e dados válidos?}
    C -- não --> B
    C -- sim --> D[Doação fica Prometida]
    D --> E[Gestor vê no feed e na página Doações]
    E --> F{Doador entregou?}
    F -- sim --> G[Gestor informa os itens recebidos]
    G --> H{Itens válidos?}
    H -- não --> G
    H -- sim --> I[Estoque soma os itens e a doação fica Recebida]
    F -- não --> J[Gestor cancela e a doação fica Cancelada]
    I --> K([Fim])
    J --> K
```

## Sequência: confirmar recebimento

```mermaid
sequenceDiagram
    actor Gestor
    participant API as DonationsController
    participant Scope as CollectionPointScopeBehavior
    participant H as ReceiveDonationHandler
    participant P as CollectionPoint
    participant D as Donation
    participant DB as UnitOfWork

    Gestor->>API: POST /collection-points/{p}/donations/{d}/receive
    API->>Scope: ReceiveDonationCommand
    Scope-->>API: 403 se o ponto não é do gestor
    Scope->>H: segue
    H->>D: carrega e confere que é do ponto
    H->>P: ReceiveStock(itens)
    P-->>H: erro se item inexistente ou quantidade <= 0
    H->>D: MarkReceived()
    D-->>H: erro se já fechada
    H->>DB: SaveChanges (estoque + doação juntos)
    H-->>Gestor: 200 DonationResponse
```
