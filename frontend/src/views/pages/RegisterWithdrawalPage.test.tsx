import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { beneficiaryService, collectionPointService, withdrawalService } from '@/models/services'
import type {
  Beneficiary,
  BeneficiaryWithdrawalHistory,
  CollectionPoint,
  Withdrawal,
} from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { RegisterWithdrawalPage } from './RegisterWithdrawalPage'

const point = {
  id: 'point-1',
  name: 'Casa Solidária',
  address: { city: 'Concórdia', state: 'SC' },
  withdrawalRule: { type: 'Annual', maxWithdrawalsPerCycle: 3 },
  stock: [
    { category: 'Food', quantity: 5 },
    { category: 'Clothing', quantity: 6 },
    { category: 'Footwear', quantity: 4 },
  ],
} as CollectionPoint

const maria: Beneficiary = {
  id: 'ben-1',
  name: 'Maria Souza',
  cpf: '111.444.777-35',
  email: 'maria@example.com',
  phones: [],
  linkedPointIds: ['point-1'],
  isActive: true,
  kinships: [],
}

function historyWith(
  used: number,
  withdrawals: BeneficiaryWithdrawalHistory['withdrawals'] = [],
): BeneficiaryWithdrawalHistory {
  return {
    beneficiaryId: 'ben-1',
    beneficiaryName: 'Maria Souza',
    limitedCategory: 'Food',
    cycleType: 'Annual',
    maxPerCycle: 3,
    usedInCurrentCycle: used,
    remainingInCurrentCycle: 3 - used,
    currentCycleStartUtc: '2026-01-01T00:00:00Z',
    currentCycleEndUtc: '2027-01-01T00:00:00Z',
    withdrawals,
  }
}

beforeEach(() => {
  useAuthStore.setState({
    user: {
      userId: 'u1',
      email: 'g@x.com',
      fullName: 'Gestor',
      roles: ['PointManager'],
      collectionPointId: 'point-1',
    },
    status: 'authenticated',
  })
  vi.spyOn(collectionPointService, 'listPublic').mockResolvedValue([point])
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <RegisterWithdrawalPage />
    </QueryClientProvider>,
  )
}

async function searchMaria() {
  await screen.findByText(/Limite deste ponto/)
  await userEvent.type(screen.getByLabelText('CPF do beneficiário'), '11144477735{Enter}')
}

describe('RegisterWithdrawalPage', () => {
  it('quando o gestor tem um só ponto, mostra o campo já selecionado e desabilitado', async () => {
    renderPage()

    const pointField = await screen.findByRole('combobox', { name: /Ponto da retirada/ })

    expect(pointField).toBeDisabled()
    await waitFor(() => expect(pointField).toHaveTextContent('Casa Solidária'))
  })

  it('busca pelo CPF na própria tela, com máscara, e mostra o saldo do ciclo', async () => {
    const findByCpf = vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(1))
    renderPage()

    await screen.findByText(/Limite deste ponto/)
    const input = screen.getByLabelText('CPF do beneficiário')
    await userEvent.type(input, '11144477735')
    expect(input).toHaveValue('111.444.777-35')
    await userEvent.type(input, '{Enter}')

    expect(await screen.findByText('Maria Souza')).toBeInTheDocument()
    expect(findByCpf).toHaveBeenCalledWith('111.444.777-35')
    expect(await screen.findByText(/Cestas básicas no ciclo: 1 de 3/)).toBeInTheDocument()
    expect(screen.getByText('2 restantes')).toBeInTheDocument()
    expect(screen.getByText(/Ciclo atual: 01\/01\/2026 a 31\/12\/2026/)).toBeInTheDocument()
  })

  it('só habilita a busca com o CPF completo', async () => {
    renderPage()
    await screen.findByText(/Limite deste ponto/)

    await userEvent.type(screen.getByLabelText('CPF do beneficiário'), '1114447')

    expect(screen.getByRole('button', { name: 'Buscar' })).toBeDisabled()
  })

  it('avisa quando o CPF não é encontrado', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockRejectedValue(new Error('404'))
    renderPage()

    await searchMaria()

    expect(await screen.findByText(/Nenhum beneficiário encontrado/)).toBeInTheDocument()
  })

  it('bloqueia o registro quando o limite do ciclo foi atingido', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(3))
    renderPage()

    await searchMaria()

    expect(await screen.findByText('Limite atingido')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('registra sem modal e volta à busca para o próximo beneficiário', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-1',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    await userEvent.type(await screen.findByLabelText(/Observações/), 'Uma cesta')
    await userEvent.click(screen.getByRole('button', { name: /registrar retirada/i }))

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        categories: ['Food'],
        notes: 'Uma cesta',
      }),
    )
    expect(await screen.findByText('Retirada registrada!')).toBeInTheDocument()
    expect(screen.getByLabelText('CPF do beneficiário')).toHaveValue('')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('lista todo tipo de retirada do beneficiário, com a categoria de cada uma', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(1, [
        {
          id: 'w-2',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          categories: ['Clothing', 'Footwear'],
          occurredAtUtc: '2026-08-10T15:00:00Z',
          notes: 'Duas camisetas',
        },
        {
          id: 'w-1',
          pointId: 'other-point',
          beneficiaryId: 'ben-1',
          categories: ['Food'],
          occurredAtUtc: '2026-03-02T15:00:00Z',
          notes: null,
        },
      ]),
    )
    renderPage()

    await searchMaria()

    const history = await screen.findByRole('region', { name: 'Histórico de retiradas' })
    expect(history).toHaveTextContent('Roupas')
    expect(history).toHaveTextContent('Cestas básicas')
    expect(history).toHaveTextContent('Duas camisetas')
    expect(history).toHaveTextContent('Neste ponto')
    expect(history).toHaveTextContent('Em outro ponto')
  })

  it('registra vários itens de uma vez: cesta básica, roupas e calçados', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-8',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      categories: ['Food', 'Clothing', 'Footwear'],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    await userEvent.click(await screen.findByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Roupas (6)' }))
    await userEvent.click(await screen.findByRole('option', { name: 'Calçados (4)' }))
    await userEvent.keyboard('{Escape}')
    await userEvent.click(screen.getByRole('button', { name: /registrar retirada/i }))

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        categories: ['Food', 'Clothing', 'Footwear'],
        notes: null,
      }),
    )
  })

  it('com o limite de cestas esgotado, bloqueia enquanto houver cestas na lista e libera sem elas', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(3))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-9',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      categories: ['Clothing'],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    expect(await screen.findByRole('button', { name: /registrar retirada/i })).toBeDisabled()

    await userEvent.click(screen.getByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Roupas (6)' }))
    await userEvent.keyboard('{Escape}')
    await userEvent.click(screen.getByRole('button', { name: 'Remover Cestas básicas (5)' }))

    const button = screen.getByRole('button', { name: /registrar retirada/i })
    expect(button).toBeEnabled()
    expect(screen.getByText(/Sem limite por ciclo/)).toBeInTheDocument()
    await userEvent.click(button)

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        categories: ['Clothing'],
        notes: null,
      }),
    )
  })

  it('quando o ponto não tem nenhum item em estoque, bloqueia o registro', async () => {
    const emptyPoint = { ...point, stock: [] } as CollectionPoint
    vi.spyOn(collectionPointService, 'listPublic').mockResolvedValue([emptyPoint])
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()

    expect(
      await screen.findByText('Este ponto não tem nenhum item em estoque no momento.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /Itens da retirada/ })).toHaveAttribute(
      'aria-disabled',
      'true',
    )
    expect(screen.getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('não permite escolher um item que o ponto não tem em estoque', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    await userEvent.click(screen.getByRole('combobox', { name: /Itens da retirada/ }))

    expect(screen.queryByRole('option', { name: /Brinquedos/ })).not.toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Roupas (6)' })).toBeInTheDocument()
  })
})
