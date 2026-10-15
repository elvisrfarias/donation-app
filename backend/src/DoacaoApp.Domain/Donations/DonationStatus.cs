namespace DoacaoApp.Domain.Donations;

/// <summary>Situação de uma doação prometida por alguém da comunidade.</summary>
public enum DonationStatus
{
    Pledged = 1,
    Received = 2,
    Cancelled = 3,
}
