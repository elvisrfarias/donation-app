import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { z } from 'zod'
import { FormComponent, type FormFieldConfig } from '.'

const fields: FormFieldConfig[] = [{ name: 'password', type: 'password', label: 'Senha' }]

afterEach(cleanup)

function renderForm() {
  render(
    <FormComponent
      schema={z.object({ password: z.string() })}
      defaultValues={{ password: '' }}
      fields={fields}
      onSubmit={() => undefined}
      submitLabel="Entrar"
    />,
  )
}

describe('visibilidade do campo de senha', () => {
  it('começa com a senha oculta', () => {
    renderForm()

    expect(screen.getByLabelText('Senha')).toHaveAttribute('type', 'password')
    expect(screen.getByRole('button', { name: 'Mostrar senha' })).toHaveAttribute(
      'aria-pressed',
      'false',
    )
  })

  it('mostra e volta a ocultar a senha ao clicar no olho, mantendo o que foi digitado', async () => {
    renderForm()
    const input = screen.getByLabelText('Senha')
    await userEvent.type(input, 'Segredo@1')

    await userEvent.click(screen.getByRole('button', { name: 'Mostrar senha' }))

    expect(input).toHaveAttribute('type', 'text')
    expect(input).toHaveValue('Segredo@1')

    await userEvent.click(screen.getByRole('button', { name: 'Ocultar senha' }))

    expect(input).toHaveAttribute('type', 'password')
  })

  it('não mostra o olho em campos que não são de senha', () => {
    render(
      <FormComponent
        schema={z.object({ name: z.string() })}
        defaultValues={{ name: '' }}
        fields={[{ name: 'name', type: 'text', label: 'Nome' }]}
        onSubmit={() => undefined}
        submitLabel="Salvar"
      />,
    )

    expect(screen.queryByRole('button', { name: /senha/i })).not.toBeInTheDocument()
  })
})
