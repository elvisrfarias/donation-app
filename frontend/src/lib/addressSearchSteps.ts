import { formatAddress, type Address } from '@/models/types'

export interface AddressSearchStep {
  query: string
  approximate: boolean
}

/**
 * Consultas de busca do mais preciso ao menos preciso: endereço completo; só rua, número e cidade
 * (o bairro e o CEP costumam não bater com o OpenStreetMap); e por fim o centro da cidade, que
 * serve de aproximação para o pin existir e o mapa ter para onde ir.
 */
export function addressSearchSteps(address: Address): AddressSearchStep[] {
  const cityAndState = `${address.city} - ${address.state}`
  const streetAndNumber = `${address.street}, ${address.number}, ${cityAndState}`

  const steps: AddressSearchStep[] = [
    { query: formatAddress(address), approximate: false },
    { query: streetAndNumber, approximate: false },
    { query: cityAndState, approximate: true },
  ]

  return steps.filter(
    (step, index) =>
      step.query.trim() !== '' && steps.findIndex((other) => other.query === step.query) === index,
  )
}
