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
  description: z.string().max(2000, 'Máximo de 2000 caracteres.').optional(),
  openingHours: z.string().max(200, 'Máximo de 200 caracteres.').optional(),
  needs: z.string().max(2000, 'Máximo de 2000 caracteres.').optional(),
  street: z.string().min(1, 'Rua é obrigatória.').max(200),
  number: z.string().min(1, 'Número é obrigatório.').max(20),
  complement: z.string().max(120).optional(),
  district: z.string().min(1, 'Bairro é obrigatório.').max(120),
  city: z.string().min(1, 'Cidade é obrigatória.').max(120),
  state: z.string().length(2, 'UF deve ter 2 letras.'),
  zipCode: z.string().min(8, 'CEP inválido (8 dígitos).').max(9),
  latitude: z.number().min(-90).max(90).optional(),
  longitude: z.number().min(-180).max(180).optional(),
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

/** Edição de ponto de coleta: mesmos campos, mas a imagem é opcional (mantém a
 * atual quando nenhum arquivo novo é escolhido). */
export const updateCollectionPointSchema = createCollectionPointSchema.extend({
  image: imageFileSchema.optional(),
})
export type UpdateCollectionPointForm = z.infer<typeof updateCollectionPointSchema>

const PHONE_DIGITS = [10, 11]

const optionalPhoneSchema = z
  .string()
  .refine(
    (value) => value === '' || PHONE_DIGITS.includes(value.replace(/\D/g, '').length),
    'Telefone inválido.',
  )
  .optional()

const requiredPhoneSchema = z
  .string()
  .min(1, 'Informe o telefone de contato.')
  .refine((value) => PHONE_DIGITS.includes(value.replace(/\D/g, '').length), 'Telefone inválido.')

const beneficiaryPersonShape = {
  name: z.string().min(1, 'Nome é obrigatório.').max(160),
  cpf: z.string().min(11, 'CPF inválido (mín. 11 dígitos).').max(14),
  email: z.string().min(1, 'E-mail é obrigatório.').max(320).email('E-mail inválido.'),
  birthDate: z.string().min(1, 'Data de nascimento é obrigatória.'),
  phone: requiredPhoneSchema,
  secondaryPhone: optionalPhoneSchema,
}

const ZIP_CODE_DIGITS = 8
const STATE_LENGTH = 2
const ADDRESS_REQUIRED_MESSAGE = 'Obrigatório quando o endereço é informado.'

const optionalText = (max: number) => z.string().max(max).optional()

const beneficiaryAddressShape = {
  zipCode: optionalText(9),
  street: optionalText(200),
  number: optionalText(20),
  complement: optionalText(120),
  district: optionalText(120),
  city: optionalText(120),
  state: optionalText(STATE_LENGTH),
}

const addressRequiredFields = ['zipCode', 'street', 'number', 'district', 'city', 'state'] as const

interface BeneficiaryContactFields {
  phone?: string
  secondaryPhone?: string
  zipCode?: string
  street?: string
  number?: string
  complement?: string
  district?: string
  city?: string
  state?: string
}

function digitsOf(value?: string): string {
  return (value ?? '').replace(/\D/g, '')
}

function isAddressStarted(values: BeneficiaryContactFields): boolean {
  return [...addressRequiredFields, 'complement' as const].some((field) => !!values[field]?.trim())
}

function checkBeneficiaryContacts(values: BeneficiaryContactFields, ctx: z.RefinementCtx) {
  if (digitsOf(values.phone) && digitsOf(values.phone) === digitsOf(values.secondaryPhone)) {
    ctx.addIssue({
      code: 'custom',
      message: 'Informe telefones diferentes.',
      path: ['secondaryPhone'],
    })
  }
  if (!isAddressStarted(values)) return

  for (const field of addressRequiredFields) {
    if (!values[field]?.trim()) {
      ctx.addIssue({ code: 'custom', message: ADDRESS_REQUIRED_MESSAGE, path: [field] })
    }
  }
  if (values.zipCode?.trim() && digitsOf(values.zipCode).length !== ZIP_CODE_DIGITS) {
    ctx.addIssue({ code: 'custom', message: 'CEP inválido (8 dígitos).', path: ['zipCode'] })
  }
  if (values.state?.trim() && values.state.trim().length !== STATE_LENGTH) {
    ctx.addIssue({ code: 'custom', message: 'UF deve ter 2 letras.', path: ['state'] })
  }
}

export const registerBeneficiarySchema = z
  .object({
    ...beneficiaryPersonShape,
    ...beneficiaryAddressShape,
    registeredByPointId: z.string().min(1, 'Selecione o ponto de cadastro.'),
  })
  .superRefine(checkBeneficiaryContacts)
export type RegisterBeneficiaryForm = z.infer<typeof registerBeneficiarySchema>

export const updateBeneficiarySchema = z
  .object({ ...beneficiaryPersonShape, ...beneficiaryAddressShape })
  .superRefine(checkBeneficiaryContacts)
export type UpdateBeneficiaryForm = z.infer<typeof updateBeneficiarySchema>

export const addBeneficiaryKinshipSchema = z.object({
  relatedBeneficiaryCpf: z.string().min(11, 'CPF inválido (mín. 11 dígitos).').max(14),
  type: z.enum([
    'Conjuge',
    'Pai',
    'Filho',
    'Irmao',
    'Avo',
    'Neto',
    'Tio',
    'Sobrinho',
    'Primo',
    'Sogro',
    'GenroOuNora',
    'Agregado',
    'Outro',
  ]),
})
export type AddBeneficiaryKinshipForm = z.infer<typeof addBeneficiaryKinshipSchema>

export const createReviewSchema = z.object({
  authorName: z.string().min(1, 'Informe seu nome.').max(120),
  rating: z
    .number({ message: 'Selecione uma nota.' })
    .int()
    .min(1, 'Selecione uma nota.')
    .max(5, 'A nota máxima é 5.'),
  comment: z.string().max(1000, 'Máximo de 1000 caracteres.').optional(),
})
export type CreateReviewForm = z.infer<typeof createReviewSchema>

const userRoleSchema = z.enum(['Admin', 'PointManager', 'Viewer'])

function requiresCollectionPoint(role: z.infer<typeof userRoleSchema>): boolean {
  return role === 'PointManager' || role === 'Viewer'
}

export const createUserSchema = z
  .object({
    email: z.email('Informe um e-mail válido.'),
    password: z.string().min(8, 'Mínimo de 8 caracteres.').max(128),
    fullName: z.string().min(1, 'Nome é obrigatório.').max(160),
    role: userRoleSchema,
    collectionPointId: z.string().optional(),
  })
  .refine((v) => !requiresCollectionPoint(v.role) || !!v.collectionPointId, {
    message: 'Selecione o ponto de coleta deste usuário.',
    path: ['collectionPointId'],
  })
export type CreateUserForm = z.infer<typeof createUserSchema>

export const updateUserSchema = z
  .object({
    fullName: z.string().min(1, 'Nome é obrigatório.').max(160),
    role: userRoleSchema,
    collectionPointId: z.string().optional(),
  })
  .refine((v) => !requiresCollectionPoint(v.role) || !!v.collectionPointId, {
    message: 'Selecione o ponto de coleta deste usuário.',
    path: ['collectionPointId'],
  })
export type UpdateUserForm = z.infer<typeof updateUserSchema>

export const registerWithdrawalSchema = z.object({
  notes: z.string().max(500, 'Máximo de 500 caracteres.').optional(),
})
export type RegisterWithdrawalForm = z.infer<typeof registerWithdrawalSchema>
