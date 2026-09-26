import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { WhatsAppLink } from './WhatsAppLink'

afterEach(cleanup)

describe('WhatsAppLink', () => {
  it('abre a conversa do telefone no WhatsApp em outra aba', () => {
    render(<WhatsAppLink phone="(11) 98765-4321" personName="Maria Souza" />)

    const link = screen.getByRole('link', { name: /Maria Souza no WhatsApp/ })
    expect(link).toHaveAttribute('href', 'https://wa.me/5511987654321')
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', expect.stringContaining('noopener'))
  })
})
