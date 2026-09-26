import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Beneficiary } from '@/models/types'
import { ExistingBeneficiaryNotice } from './ExistingBeneficiaryNotice'

const maria: Beneficiary = {
  id: 'ben-1',
  name: 'Maria Souza',
  cpf: '111.444.777-35',
  email: 'maria@example.com',
  phones: [],
  linkedPointIds: ['other-point'],
  isActive: true,
  kinships: [],
}

afterEach(cleanup)

function renderNotice(overrides: Partial<Parameters<typeof ExistingBeneficiaryNotice>[0]> = {}) {
  const onLink = vi.fn()
  const onEdit = vi.fn()
  render(
    <ExistingBeneficiaryNotice
      beneficiary={maria}
      pointName="Casa Solidária"
      alreadyLinked={false}
      isLinking={false}
      onLink={onLink}
      onEdit={onEdit}
      {...overrides}
    />,
  )
  return { onLink, onEdit }
}

describe('ExistingBeneficiaryNotice', () => {
  it('oferece vincular a pessoa já cadastrada por outro ponto ao ponto do gestor', async () => {
    const { onLink } = renderNotice()

    expect(screen.getByText('Maria Souza')).toBeInTheDocument()
    expect(screen.getByText(/é atendida por outro ponto/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /vincular a Casa Solidária/i }))

    expect(onLink).toHaveBeenCalledTimes(1)
  })

  it('quando já está vinculada ao ponto, oferece editar os dados em vez de vincular', async () => {
    const { onEdit } = renderNotice({ alreadyLinked: true })

    expect(screen.queryByRole('button', { name: /vincular/i })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /editar dados/i }))

    expect(onEdit).toHaveBeenCalledTimes(1)
  })

  it('mostra o erro do vínculo', () => {
    renderNotice({ errorMessage: 'Não foi possível vincular.' })

    expect(screen.getByText('Não foi possível vincular.')).toBeInTheDocument()
  })
})
