import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { z } from 'zod'
import { FormComponent } from '@/views/components/form'
import { FormField, Input } from '.'

afterEach(cleanup)

function requiredMarksIn(container: HTMLElement) {
  return container.querySelectorAll<HTMLElement>('[data-required-mark]')
}

describe('RequiredMark', () => {
  it('FormField obrigatório mostra o asterisco em vermelho fixo', () => {
    const { container } = render(
      <FormField label="CPF" htmlFor="cpf" required>
        <Input id="cpf" />
      </FormField>,
    )

    const [mark] = requiredMarksIn(container)
    expect(mark).toBeDefined()
    expect(getComputedStyle(mark).color).toBe('rgb(220, 38, 38)')
  })

  it('FormField opcional não mostra o asterisco', () => {
    const { container } = render(
      <FormField label="Observações" htmlFor="notes">
        <Input id="notes" />
      </FormField>,
    )

    expect(requiredMarksIn(container)).toHaveLength(0)
  })

  it('o asterisco não entra no nome do campo', () => {
    render(
      <FormField label="CPF" htmlFor="cpf" required>
        <Input id="cpf" />
      </FormField>,
    )

    expect(screen.getByLabelText('CPF')).toBeInTheDocument()
  })

  it('campos obrigatórios do FormComponent também mostram o asterisco vermelho', () => {
    const { container } = render(
      <FormComponent
        schema={z.object({ name: z.string(), nickname: z.string() })}
        defaultValues={{ name: '', nickname: '' }}
        fields={[
          { name: 'name', type: 'text', label: 'Nome', required: true },
          { name: 'nickname', type: 'text', label: 'Apelido' },
        ]}
        onSubmit={() => undefined}
        submitLabel="Salvar"
      />,
    )

    const marks = requiredMarksIn(container)
    expect(marks).toHaveLength(1)
    expect(getComputedStyle(marks[0]).color).toBe('rgb(220, 38, 38)')
  })
})
