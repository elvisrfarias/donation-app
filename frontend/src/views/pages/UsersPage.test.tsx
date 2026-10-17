import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { collectionPointService, userService } from '@/models/services'
import type { UserAccount } from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { UsersPage } from './UsersPage'

const joana: UserAccount = {
  id: 'user-joana',
  email: 'joana@dooa.com.br',
  fullName: 'Joana Lima',
  roles: ['Admin'],
  collectionPointId: null,
  isActive: true,
  phoneNumber: '(49) 3442-1234',
}

beforeEach(() => {
  useAuthStore.setState({
    user: {
      userId: 'admin-1',
      email: 'admin@dooa.com.br',
      fullName: 'Admin',
      roles: ['Admin'],
      collectionPointId: null,
    },
  })
  vi.spyOn(userService, 'list').mockResolvedValue([joana])
  vi.spyOn(collectionPointService, 'listForManagement').mockResolvedValue([])
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <UsersPage />
    </QueryClientProvider>,
  )
}

describe('UsersPage', () => {
  it('mostra o telefone do usuário na lista', async () => {
    renderPage()

    expect((await screen.findAllByText('(49) 3442-1234')).length).toBeGreaterThan(0)
  })

  it('permite editar e-mail e telefone do usuário', async () => {
    const update = vi.spyOn(userService, 'update').mockResolvedValue(joana)
    renderPage()

    await userEvent.click((await screen.findAllByRole('button', { name: 'Editar' }))[0])
    const email = screen.getByLabelText(/^E-mail/)
    const phone = screen.getByLabelText(/^Telefone/)
    expect(email).toHaveValue('joana@dooa.com.br')
    expect(phone).toHaveValue('(49) 3442-1234')

    await userEvent.clear(email)
    await userEvent.type(email, 'joana.lima@dooa.com.br')
    await userEvent.clear(phone)
    await userEvent.type(phone, '49998765432')
    await userEvent.click(screen.getByRole('button', { name: /Salvar alterações/ }))

    await waitFor(() =>
      expect(update).toHaveBeenCalledWith(
        'user-joana',
        expect.objectContaining({
          email: 'joana.lima@dooa.com.br',
          phoneNumber: '(49) 99876-5432',
          fullName: 'Joana Lima',
          role: 'Admin',
        }),
      ),
    )
  })

  it('recusa telefone incompleto', async () => {
    const update = vi.spyOn(userService, 'update')
    renderPage()

    await userEvent.click((await screen.findAllByRole('button', { name: 'Editar' }))[0])
    const phone = screen.getByLabelText(/^Telefone/)
    await userEvent.clear(phone)
    await userEvent.type(phone, '4999')
    await userEvent.click(screen.getByRole('button', { name: /Salvar alterações/ }))

    expect(await screen.findByText('Telefone inválido.')).toBeInTheDocument()
    expect(update).not.toHaveBeenCalled()
  })
})
