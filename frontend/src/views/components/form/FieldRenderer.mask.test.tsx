import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { z } from 'zod'
import { FormComponent, type FormFieldConfig } from '.'

const schema = z.object({ cpf: z.string(), phone: z.string(), name: z.string() })

const fields: FormFieldConfig[] = [
  { name: 'name', type: 'text', label: 'Nome' },
  { name: 'cpf', type: 'text', mask: 'cpf', label: 'CPF' },
  { name: 'phone', type: 'text', mask: 'phone', label: 'Telefone' },
]

afterEach(cleanup)

function renderForm() {
  render(
    <FormComponent
      schema={schema}
      defaultValues={{ name: '', cpf: '', phone: '' }}
      fields={fields}
      onSubmit={() => undefined}
      submitLabel="Salvar"
    />,
  )
}

describe('máscaras dos campos de texto', () => {
  it('aplica a máscara de CPF enquanto digita', async () => {
    renderForm()

    await userEvent.type(screen.getByLabelText('CPF'), '11144477735')

    expect(screen.getByLabelText('CPF')).toHaveValue('111.444.777-35')
  })

  it('aplica a máscara de telefone enquanto digita', async () => {
    renderForm()

    await userEvent.type(screen.getByLabelText('Telefone'), '11987654321')

    expect(screen.getByLabelText('Telefone')).toHaveValue('(11) 98765-4321')
  })

  it('não altera campos sem máscara', async () => {
    renderForm()

    await userEvent.type(screen.getByLabelText('Nome'), 'Maria 123')

    expect(screen.getByLabelText('Nome')).toHaveValue('Maria 123')
  })
})
