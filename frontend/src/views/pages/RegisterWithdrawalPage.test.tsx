import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
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
  vi.spyOn(collectionPointService, 'getById').mockResolvedValue(point)
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
  await screen.findByText('Maria Souza')
}

async function openAddWithdrawalModal() {
  await userEvent.click(screen.getByRole('button', { name: /adicionar retirada/i }))
  return screen.findByRole('dialog')
}

describe('RegisterWithdrawalPage', () => {
  it('quando o gestor tem um só ponto, mostra o campo já selecionado e desabilitado', async () => {
    renderPage()

    const pointField = await screen.findByRole('combobox', { name: /Ponto da retirada/ })

    expect(pointField).toBeDisabled()
    await waitFor(() => expect(pointField).toHaveTextContent('Casa Solidária'))
  })

  it('busca pelo CPF, com máscara, e mostra a lista de retiradas do beneficiário', async () => {
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
    expect(
      screen.getByText('Este beneficiário ainda não fez nenhuma retirada.'),
    ).toBeInTheDocument()
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

    await screen.findByText(/Limite deste ponto/)
    await userEvent.type(screen.getByLabelText('CPF do beneficiário'), '11144477735{Enter}')

    expect(await screen.findByText(/Nenhum beneficiário encontrado/)).toBeInTheDocument()
  })

  it('lista todo tipo de retirada do beneficiário, com a categoria e o ponto de cada uma', async () => {
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

    const table = await screen.findByRole('table')
    expect(table).toHaveTextContent('Roupas')
    expect(table).toHaveTextContent('Calçados')
    expect(table).toHaveTextContent('Cestas básicas')
    expect(table).toHaveTextContent('Duas camisetas')
    expect(table).toHaveTextContent('Neste ponto')
    expect(table).toHaveTextContent('Em outro ponto')
  })

  it('abre o modal de Adicionar retirada sem nenhum item pré-selecionado', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()

    expect(within(dialog).queryByRole('button', { name: /^Remover/ })).not.toBeInTheDocument()
    expect(within(dialog).getByText('Escolha ao menos um item disponível.')).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('bloqueia o registro ao escolher cesta básica com o limite do ciclo atingido', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(3))
    renderPage()

    await searchMaria()
    expect(screen.getByText('Limite atingido')).toBeInTheDocument()

    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Cestas básicas (5)' }))
    await userEvent.keyboard('{Escape}')

    expect(within(dialog).getByText(/atingiu o limite de cestas básicas/)).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('registra pela modal, fecha ao concluir e mantém o beneficiário para a próxima retirada', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-1',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      categories: ['Food'],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Cestas básicas (5)' }))
    await userEvent.keyboard('{Escape}')
    await userEvent.type(within(dialog).getByLabelText(/Observações/), 'Uma cesta')
    await userEvent.click(within(dialog).getByRole('button', { name: /registrar retirada/i }))

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        categories: ['Food'],
        notes: 'Uma cesta',
      }),
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await screen.findByText('Retirada registrada!')).toBeInTheDocument()
    expect(screen.getByText('Maria Souza')).toBeInTheDocument()
    expect(screen.queryByLabelText('CPF do beneficiário')).not.toBeInTheDocument()
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
    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Cestas básicas (5)' }))
    await userEvent.click(await screen.findByRole('option', { name: 'Roupas (6)' }))
    await userEvent.click(await screen.findByRole('option', { name: 'Calçados (4)' }))
    await userEvent.keyboard('{Escape}')
    await userEvent.click(within(dialog).getByRole('button', { name: /registrar retirada/i }))

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
    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Cestas básicas (5)' }))
    await userEvent.keyboard('{Escape}')
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()

    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
    await userEvent.click(await screen.findByRole('option', { name: 'Roupas (6)' }))
    await userEvent.keyboard('{Escape}')
    await userEvent.click(
      within(dialog).getByRole('button', { name: 'Remover Cestas básicas (5)' }),
    )

    const button = within(dialog).getByRole('button', { name: /registrar retirada/i })
    expect(button).toBeEnabled()
    expect(within(dialog).getByText(/Sem limite por ciclo/)).toBeInTheDocument()
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

  it('quando o ponto não tem nenhum item em estoque, bloqueia o registro na modal', async () => {
    const emptyPoint = { ...point, stock: [] } as CollectionPoint
    vi.spyOn(collectionPointService, 'listPublic').mockResolvedValue([emptyPoint])
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue(emptyPoint)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()

    expect(
      within(dialog).getByText('Este ponto não tem nenhum item em estoque no momento.'),
    ).toBeInTheDocument()
    expect(within(dialog).getByRole('combobox', { name: /Itens da retirada/ })).toHaveAttribute(
      'aria-disabled',
      'true',
    )
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('não permite escolher um item que o ponto não tem em estoque', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))

    expect(screen.queryByRole('option', { name: /Brinquedos/ })).not.toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Roupas (6)' })).toBeInTheDocument()
  })

  it('não lista uma categoria com estoque se o ponto não a está acompanhando', async () => {
    const pointNotTrackingFootwear = {
      ...point,
      stockSettings: [
        { category: 'Food', goal: 0, alertThreshold: 5, isTracked: true },
        { category: 'Clothing', goal: 0, alertThreshold: 5, isTracked: true },
        { category: 'Footwear', goal: 0, alertThreshold: 5, isTracked: false },
      ],
    } as CollectionPoint
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue(pointNotTrackingFootwear)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))

    expect(screen.getByRole('option', { name: 'Cestas básicas (5)' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Roupas (6)' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: /Calçados/ })).not.toBeInTheDocument()
  })

  it('busca o estoque atualizado do ponto ao abrir o modal de adicionar retirada', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const getById = vi.spyOn(collectionPointService, 'getById').mockResolvedValue(point)
    renderPage()

    await searchMaria()
    const callsBeforeOpening = getById.mock.calls.length
    await openAddWithdrawalModal()

    await waitFor(() => expect(getById.mock.calls.length).toBeGreaterThan(callsBeforeOpening))
  })

  it('só mostra o botão de editar na retirada mais recente feita neste ponto', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(0, [
        {
          id: 'w-recent',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          categories: ['Clothing'],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: null,
        },
        {
          id: 'w-older',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          categories: ['Food'],
          occurredAtUtc: '2026-03-02T15:00:00Z',
          notes: null,
        },
      ]),
    )
    renderPage()

    await searchMaria()
    const table = await screen.findByRole('table')
    const rows = within(table).getAllByRole('row').slice(1)

    expect(within(rows[0]).getByRole('button', { name: 'Editar retirada' })).toBeInTheDocument()
    expect(
      within(rows[1]).queryByRole('button', { name: 'Editar retirada' }),
    ).not.toBeInTheDocument()
  })

  it('não mostra o botão de editar quando a retirada mais recente do beneficiário foi em outro ponto', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(0, [
        {
          id: 'w-other',
          pointId: 'other-point',
          beneficiaryId: 'ben-1',
          categories: ['Clothing'],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: null,
        },
        {
          id: 'w-here',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          categories: ['Food'],
          occurredAtUtc: '2026-03-02T15:00:00Z',
          notes: null,
        },
      ]),
    )
    renderPage()

    await searchMaria()
    const table = await screen.findByRole('table')

    expect(within(table).queryByRole('button', { name: 'Editar retirada' })).not.toBeInTheDocument()
  })

  it('edita a retirada mais recente, pré-preenchendo os itens e as observações atuais', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(0, [
        {
          id: 'w-recent',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          categories: ['Clothing', 'Footwear'],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: 'Nota original',
        },
      ]),
    )
    const update = vi.spyOn(withdrawalService, 'update').mockResolvedValue({
      id: 'w-recent',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      categories: ['Clothing'],
      occurredAtUtc: '2026-09-26T15:00:00Z',
      notes: 'Nota corrigida',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    await userEvent.click(await screen.findByRole('button', { name: 'Editar retirada' }))
    const dialog = await screen.findByRole('dialog', { name: /editar retirada/i })

    expect(within(dialog).getByText('Roupas (6)')).toBeInTheDocument()
    expect(within(dialog).getByText('Calçados (4)')).toBeInTheDocument()
    expect(within(dialog).getByLabelText(/Observações/)).toHaveValue('Nota original')

    await userEvent.click(within(dialog).getByRole('button', { name: 'Remover Calçados (4)' }))
    await userEvent.clear(within(dialog).getByLabelText(/Observações/))
    await userEvent.type(within(dialog).getByLabelText(/Observações/), 'Nota corrigida')
    await userEvent.click(within(dialog).getByRole('button', { name: /editar retirada/i }))

    await waitFor(() =>
      expect(update).toHaveBeenCalledWith('w-recent', {
        pointId: 'point-1',
        categories: ['Clothing'],
        notes: 'Nota corrigida',
      }),
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await screen.findByText('Retirada atualizada!')).toBeInTheDocument()
  })

  it('quando o backend recusa a edição, mostra o erro e mantém a modal aberta', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(0, [
        {
          id: 'w-recent',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          categories: ['Clothing'],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: null,
        },
      ]),
    )
    vi.spyOn(withdrawalService, 'update').mockRejectedValue(
      Object.assign(new Error('conflito'), {
        isConflict: true,
        displayMessage: 'Só é possível editar a retirada mais recente do beneficiário.',
      }),
    )
    renderPage()

    await searchMaria()
    await userEvent.click(await screen.findByRole('button', { name: 'Editar retirada' }))
    const dialog = await screen.findByRole('dialog', { name: /editar retirada/i })
    await userEvent.click(within(dialog).getByRole('button', { name: /editar retirada/i }))

    expect(
      await within(dialog).findByText(
        'Só é possível editar a retirada mais recente do beneficiário.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })
})
