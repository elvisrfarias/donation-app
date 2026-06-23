import { z } from 'zod'

export const loginSchema = z.object({
  email: z.email('Informe um e-mail válido.'),
  password: z.string().min(1, 'Informe a senha.'),
})
export type LoginForm = z.infer<typeof loginSchema>

/** Imagem do ponto de coleta — agora obrigatória ao cadastrar. */
const MAX_IMAGE_BYTES = 10 * 1024 * 1024
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'image/avif']
const imageFileSchema = z
  .instanceof(File, { message: 'A imagem do ponto é obrigatória.' })
  .refine((file) => file.size > 0, 'A imagem do ponto é obrigatória.')
  .refine((file) => file.size <= MAX_IMAGE_BYTES, 'A imagem excede o limite de 10 MB.')
  .refine(
    (file) => ALLOWED_IMAGE_TYPES.includes(file.type),
    'Formato não suportado. Use JPEG, PNG, WebP, GIF ou AVIF.',
  )

export const createCollectionPointSchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório.').max(200),
  type: z.enum(['Association', 'Church', 'PublicEntity']),
  street: z.string().min(1, 'Rua é obrigatória.').max(200),
  number: z.string().min(1, 'Número é obrigatório.').max(20),
  complement: z.string().max(120).optional(),
  district: z.string().min(1, 'Bairro é obrigatório.').max(120),
  city: z.string().min(1, 'Cidade é obrigatória.').max(120),
  state: z.string().length(2, 'UF deve ter 2 letras.'),
  zipCode: z.string().min(8, 'CEP inválido (8 dígitos).').max(9),
  responsibleName: z.string().min(1, 'Responsável é obrigatório.').max(160),
  email: z.email('E-mail de contato inválido.'),
  phone: z.string().min(10, 'Telefone inválido.').max(15),
  acceptedItems: z
    .array(
      z.enum([
        'Food',
        'Clothing',
        'Footwear',
        'Hygiene',
        'Cleaning',
        'Furniture',
        'Appliances',
        'Toys',
        'Books',
        'Medicine',
        'Other',
      ]),
    )
    .min(1, 'Selecione ao menos uma categoria de itens.'),
  image: imageFileSchema,
})
export type CreateCollectionPointForm = z.infer<typeof createCollectionPointSchema>

export const createFamilySchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório.').max(200),
  cycleType: z.enum(['Weekly', 'Biweekly', 'Monthly']),
  maxWithdrawalsPerCycle: z
    .number({ message: 'Informe um número.' })
    .int('Use um número inteiro.')
    .min(1, 'Mínimo de 1 por ciclo.')
    .max(100, 'Máximo de 100.'),
})
export type CreateFamilyForm = z.infer<typeof createFamilySchema>

export const registerBeneficiarySchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório.').max(160),
  cpf: z.string().min(11, 'CPF inválido (mín. 11 dígitos).').max(14),
  birthDate: z.string().optional(),
  registeredByPointId: z.string().min(1, 'Selecione o ponto de cadastro.'),
})
export type RegisterBeneficiaryForm = z.infer<typeof registerBeneficiarySchema>

export const registerWithdrawalSchema = z.object({
  pointId: z.string().min(1, 'Selecione o ponto de coleta.'),
  familyId: z.string().min(1, 'Informe o ID da família.'),
  beneficiaryId: z.string().min(1, 'Selecione o beneficiário.'),
  notes: z.string().max(500, 'Máximo de 500 caracteres.').optional(),
})
export type RegisterWithdrawalForm = z.infer<typeof registerWithdrawalSchema>
