import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { beneficiaryService, collectionPointService, withdrawalService } from '@/models/services'
import type {
  BasketTemplate,
  Beneficiary,
  BeneficiaryWithdrawalHistory,
  CollectionPoint,
  Withdrawal,
  WithdrawalItemRef,
} from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { RegisterWithdrawalPage } from './RegisterWithdrawalPage'

const foodCategory = { id: 'cat-food', name: 'Alimentos', icon: 'apple' }
const clothingCategory = { id: 'cat-clothing', name: 'Roupas', icon: 'shirt' }
const footwearCategory = { id: 'cat-footwear', name: 'Calçados', icon: 'footprints' }

const riceItem = {
  id: 'item-food',
  name: 'Arroz 1kg',
  unit: 'kg',
  categoryId: foodCategory.id,
  categoryName: foodCategory.name,
  categoryIcon: foodCategory.icon,
  quantity: 5,
  goal: 0,
  alertThreshold: 5,
  isTracked: true,
  status: 'ok',
}

const clothingItem = {
  id: 'item-clothing',
  name: 'Camiseta',
  unit: 'un',
  categoryId: clothingCategory.id,
  categoryName: clothingCategory.name,
  categoryIcon: clothingCategory.icon,
  quantity: 6,
  goal: 0,
  alertThreshold: 5,
  isTracked: true,
  status: 'ok',
}

const footwearItem = {
  id: 'item-footwear',
  name: 'Tênis',
  unit: 'un',
  categoryId: footwearCategory.id,
  categoryName: footwearCategory.name,
  categoryIcon: footwearCategory.icon,
  quantity: 4,
  goal: 0,
  alertThreshold: 5,
  isTracked: true,
  status: 'ok',
}

function itemRef(item: typeof riceItem, quantity: number): WithdrawalItemRef {
  return {
    stockItemId: item.id,
    itemName: item.name,
    unit: item.unit,
    categoryId: item.categoryId,
    categoryName: item.categoryName,
    categoryIcon: item.categoryIcon,
    quantity,
  }
}

const point = {
  id: 'point-1',
  name: 'Casa Solidária',
  address: { city: 'Concórdia', state: 'SC' },
  withdrawalRule: { type: 'Annual', maxWithdrawalsPerCycle: 3 },
  basketTemplates: [],
  categories: [foodCategory, clothingCategory, footwearCategory],
  stockItems: [riceItem, clothingItem, footwearItem],
} as unknown as CollectionPoint

const basicBasket: BasketTemplate = {
  id: 'basket-1',
  name: 'Cesta básica padrão',
  isActive: true,
  items: [{ stockItemId: 'item-food', itemName: 'Arroz 1kg', quantity: 1 }],
  feasibility: {
    completeBaskets: 5,
    missing: [
      {
        itemId: 'item-food',
        itemName: 'Arroz 1kg',
        requiredPerBasket: 1,
        currentQuantity: 5,
        neededForNextBasket: 0,
      },
    ],
  },
}

const pointWithBasket: CollectionPoint = { ...point, basketTemplates: [basicBasket] }

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
      <MemoryRouter>
        <RegisterWithdrawalPage />
      </MemoryRouter>
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

async function addItem(dialog: HTMLElement, optionName: string | RegExp) {
  await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
  await userEvent.click(await screen.findByRole('option', { name: optionName }))
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

  it('lista todo tipo de retirada do beneficiário, com os itens e o ponto de cada uma', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(1, [
        {
          id: 'w-2',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          items: [itemRef(clothingItem, 2), itemRef(footwearItem, 1)],
          occurredAtUtc: '2026-08-10T15:00:00Z',
          notes: 'Duas camisetas',
        },
        {
          id: 'w-1',
          pointId: 'other-point',
          beneficiaryId: 'ben-1',
          items: [itemRef(riceItem, 3)],
          occurredAtUtc: '2026-03-02T15:00:00Z',
          notes: null,
        },
      ]),
    )
    renderPage()

    await searchMaria()

    const table = await screen.findByRole('table')
    expect(table).toHaveTextContent('Camiseta × 2')
    expect(table).toHaveTextContent('Tênis × 1')
    expect(table).toHaveTextContent('Arroz 1kg × 3')
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
    expect(within(dialog).getByText('Nenhum item adicionado ainda.')).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('adiciona um item pelo seletor, mostra a linha com stepper e permite remover', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Camiseta \(un\)/)

    expect(within(dialog).getByText('Camiseta')).toBeInTheDocument()
    expect(within(dialog).getByRole('spinbutton', { name: 'Quantidade de Camiseta' })).toHaveValue(1)
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeEnabled()

    await userEvent.click(within(dialog).getByRole('button', { name: 'Remover Camiseta' }))

    expect(within(dialog).queryByText('Camiseta')).not.toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('a quantidade de um item não passa do estoque disponível', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Tênis \(un\)/)

    const increment = within(dialog).getByRole('button', { name: 'Aumentar quantidade de Tênis' })
    for (let i = 0; i < footwearItem.quantity - 1; i++) {
      await userEvent.click(increment)
    }

    expect(within(dialog).getByRole('spinbutton', { name: 'Quantidade de Tênis' })).toHaveValue(
      footwearItem.quantity,
    )
    expect(increment).toBeDisabled()
  })

  it('não oferece mais um item já adicionado no seletor', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Camiseta \(un\)/)

    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))
    expect(screen.queryByRole('option', { name: /Camiseta/ })).not.toBeInTheDocument()
  })

  it('itens livres (roupas, calçados…) nunca são bloqueados pelo limite do ciclo', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(3))
    renderPage()

    await searchMaria()
    expect(screen.getByText('Limite atingido')).toBeInTheDocument()

    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Arroz 1kg \(kg\)/)
    await addItem(dialog, /Camiseta \(un\)/)

    expect(within(dialog).getByText(/Sem limite por ciclo/)).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeEnabled()
  })

  it('bloqueia o registro ao escolher uma cesta básica com o limite do ciclo atingido', async () => {
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue(pointWithBasket)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(3))
    renderPage()

    await searchMaria()
    expect(screen.getByText('Limite atingido')).toBeInTheDocument()

    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Camiseta \(un\)/)

    await userEvent.click(within(dialog).getByRole('combobox', { name: /Receita de cesta básica/ }))
    await userEvent.click(await screen.findByRole('option', { name: /Cesta básica padrão/ }))

    expect(
      within(dialog).getByText(/já atingiu o limite de cestas básicas do ciclo vigente/),
    ).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()

    await userEvent.click(within(dialog).getByRole('button', { name: 'Remover cesta' }))
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeEnabled()
  })

  it('registra pela modal, fecha ao concluir e mantém o beneficiário para a próxima retirada', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-1',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      items: [itemRef(riceItem, 1)],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Arroz 1kg \(kg\)/)
    await userEvent.type(within(dialog).getByLabelText(/Observações/), 'Uma cesta')
    await userEvent.click(within(dialog).getByRole('button', { name: /registrar retirada/i }))

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        items: [{ stockItemId: riceItem.id, quantity: 1 }],
        notes: 'Uma cesta',
      }),
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await screen.findByText('Retirada registrada!')).toBeInTheDocument()
    expect(screen.getByText('Maria Souza')).toBeInTheDocument()
    expect(screen.queryByLabelText('CPF do beneficiário')).not.toBeInTheDocument()
  })

  it('registra vários itens de uma vez, cada um com sua quantidade', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-8',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      items: [itemRef(riceItem, 2), itemRef(clothingItem, 1), itemRef(footwearItem, 1)],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Arroz 1kg \(kg\)/)
    await userEvent.click(
      within(dialog).getByRole('button', { name: 'Aumentar quantidade de Arroz 1kg' }),
    )
    await addItem(dialog, /Camiseta \(un\)/)
    await addItem(dialog, /Tênis \(un\)/)
    await userEvent.click(within(dialog).getByRole('button', { name: /registrar retirada/i }))

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        items: [
          { stockItemId: riceItem.id, quantity: 2 },
          { stockItemId: clothingItem.id, quantity: 1 },
          { stockItemId: footwearItem.id, quantity: 1 },
        ],
        notes: null,
      }),
    )
  })

  it('com o limite de cestas esgotado, bloqueia enquanto a cesta estiver selecionada e libera ao remover', async () => {
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue(pointWithBasket)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(3))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-9',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      items: [itemRef(clothingItem, 1)],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Camiseta \(un\)/)

    await userEvent.click(within(dialog).getByRole('combobox', { name: /Receita de cesta básica/ }))
    await userEvent.click(await screen.findByRole('option', { name: /Cesta básica padrão/ }))
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()

    await userEvent.click(within(dialog).getByRole('button', { name: 'Remover cesta' }))

    const button = within(dialog).getByRole('button', { name: /registrar retirada/i })
    expect(button).toBeEnabled()
    expect(within(dialog).getByText(/Sem limite por ciclo/)).toBeInTheDocument()
    await userEvent.click(button)

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        items: [{ stockItemId: clothingItem.id, quantity: 1 }],
        notes: null,
      }),
    )
  })

  it('registra retirada com cesta básica, envia basketTemplateId/basketQuantity e mostra as cestas restantes', async () => {
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue(pointWithBasket)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    const register = vi.spyOn(withdrawalService, 'register').mockResolvedValue({
      id: 'w-10',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      items: [itemRef(clothingItem, 1)],
      occurredAtUtc: '2026-09-26T15:00:00Z',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Camiseta \(un\)/)

    await userEvent.click(within(dialog).getByRole('combobox', { name: /Receita de cesta básica/ }))
    await userEvent.click(await screen.findByRole('option', { name: /Cesta básica padrão/ }))
    expect(within(dialog).getByText('Estoque disponível para esta cesta.')).toBeInTheDocument()

    await userEvent.click(within(dialog).getByRole('button', { name: /registrar retirada/i }))

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        pointId: 'point-1',
        beneficiaryId: 'ben-1',
        items: [{ stockItemId: clothingItem.id, quantity: 1 }],
        notes: null,
        basketTemplateId: 'basket-1',
        basketQuantity: 1,
      }),
    )
    expect(await screen.findByText(/Restam 5 cestas completas de "Cesta básica padrão"/)).toBeInTheDocument()
  })

  it('bloqueia o registro quando o estoque não é suficiente para a quantidade de cestas escolhida', async () => {
    const scarceBasket: BasketTemplate = {
      ...basicBasket,
      feasibility: {
        completeBaskets: 1,
        missing: [
          {
            itemId: 'item-food',
            itemName: 'Arroz 1kg',
            requiredPerBasket: 1,
            currentQuantity: 1,
            neededForNextBasket: 0,
          },
        ],
      },
    }
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue({
      ...point,
      basketTemplates: [scarceBasket],
    } as CollectionPoint)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await addItem(dialog, /Camiseta \(un\)/)

    await userEvent.click(within(dialog).getByRole('combobox', { name: /Receita de cesta básica/ }))
    await userEvent.click(await screen.findByRole('option', { name: /Cesta básica padrão/ }))
    await userEvent.click(within(dialog).getByRole('button', { name: 'Aumentar quantidade de cestas' }))

    expect(within(dialog).getByText('Estoque insuficiente para esta cesta')).toBeInTheDocument()
    expect(within(dialog).getByText(/Faltam 1 kg de Arroz 1kg/)).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /registrar retirada/i })).toBeDisabled()
  })

  it('quando o ponto não tem nenhum item em estoque, bloqueia o registro na modal', async () => {
    const emptyPoint = { ...point, stockItems: [] } as CollectionPoint
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
    expect(within(dialog).getByRole('combobox', { name: /Itens da retirada/ })).toBeDisabled()
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
    expect(screen.getByRole('option', { name: /Camiseta \(un\)/ })).toBeInTheDocument()
  })

  it('não lista um item com estoque se o ponto não o está acompanhando', async () => {
    const pointNotTrackingFootwear = {
      ...point,
      stockItems: point.stockItems.map((item) =>
        item.categoryId === footwearCategory.id ? { ...item, isTracked: false } : item,
      ),
    } as CollectionPoint
    vi.spyOn(collectionPointService, 'getById').mockResolvedValue(pointNotTrackingFootwear)
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(historyWith(0))
    renderPage()

    await searchMaria()
    const dialog = await openAddWithdrawalModal()
    await userEvent.click(within(dialog).getByRole('combobox', { name: /Itens da retirada/ }))

    expect(screen.getByRole('option', { name: /Arroz 1kg \(kg\)/ })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: /Camiseta \(un\)/ })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: /Tênis/ })).not.toBeInTheDocument()
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
          items: [itemRef(clothingItem, 1)],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: null,
        },
        {
          id: 'w-older',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          items: [itemRef(riceItem, 1)],
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
          items: [itemRef(clothingItem, 1)],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: null,
        },
        {
          id: 'w-here',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          items: [itemRef(riceItem, 1)],
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
          items: [itemRef(clothingItem, 2), itemRef(footwearItem, 1)],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: 'Nota original',
        },
      ]),
    )
    const update = vi.spyOn(withdrawalService, 'update').mockResolvedValue({
      id: 'w-recent',
      pointId: 'point-1',
      beneficiaryId: 'ben-1',
      items: [itemRef(clothingItem, 2)],
      occurredAtUtc: '2026-09-26T15:00:00Z',
      notes: 'Nota corrigida',
    } as Withdrawal)
    renderPage()

    await searchMaria()
    await userEvent.click(await screen.findByRole('button', { name: 'Editar retirada' }))
    const dialog = await screen.findByRole('dialog', { name: /editar retirada/i })

    expect(within(dialog).getByText('Camiseta')).toBeInTheDocument()
    expect(within(dialog).getByRole('spinbutton', { name: 'Quantidade de Camiseta' })).toHaveValue(2)
    expect(within(dialog).getByText('Tênis')).toBeInTheDocument()
    expect(within(dialog).getByLabelText(/Observações/)).toHaveValue('Nota original')

    await userEvent.click(within(dialog).getByRole('button', { name: 'Remover Tênis' }))
    await userEvent.clear(within(dialog).getByLabelText(/Observações/))
    await userEvent.type(within(dialog).getByLabelText(/Observações/), 'Nota corrigida')
    await userEvent.click(within(dialog).getByRole('button', { name: /editar retirada/i }))

    await waitFor(() =>
      expect(update).toHaveBeenCalledWith('w-recent', {
        pointId: 'point-1',
        items: [{ stockItemId: clothingItem.id, quantity: 2 }],
        notes: 'Nota corrigida',
      }),
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await screen.findByText('Retirada atualizada!')).toBeInTheDocument()
  })

  it('ao editar, permite aumentar a quantidade de um item já reservado por esta mesma retirada além do estoque livre', async () => {
    /**
     * O ponto tem só 5kg de arroz em estoque, mas esta retirada (que já está sendo editada) já
     * tinha reservado 3kg antes — então o máximo permitido no stepper é 5 + 3 = 8kg, não 5kg.
     */
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(0, [
        {
          id: 'w-recent',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          items: [itemRef(riceItem, 3)],
          occurredAtUtc: '2026-09-26T15:00:00Z',
          notes: null,
        },
      ]),
    )
    renderPage()

    await searchMaria()
    await userEvent.click(await screen.findByRole('button', { name: 'Editar retirada' }))
    const dialog = await screen.findByRole('dialog', { name: /editar retirada/i })

    const increment = within(dialog).getByRole('button', { name: 'Aumentar quantidade de Arroz 1kg' })
    for (let i = 0; i < 5; i++) {
      await userEvent.click(increment)
    }

    expect(within(dialog).getByRole('spinbutton', { name: 'Quantidade de Arroz 1kg' })).toHaveValue(8)
    expect(increment).toBeDisabled()
    expect(within(dialog).queryByText('Estoque insuficiente')).not.toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: /editar retirada/i })).toBeEnabled()
  })

  it('quando o backend recusa a edição, mostra o erro e mantém a modal aberta', async () => {
    vi.spyOn(beneficiaryService, 'findByCpf').mockResolvedValue(maria)
    vi.spyOn(beneficiaryService, 'getWithdrawalHistory').mockResolvedValue(
      historyWith(0, [
        {
          id: 'w-recent',
          pointId: 'point-1',
          beneficiaryId: 'ben-1',
          items: [itemRef(clothingItem, 1)],
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
