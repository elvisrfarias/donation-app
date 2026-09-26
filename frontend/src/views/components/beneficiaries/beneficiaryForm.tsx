import type { AnyFormApi } from '@tanstack/react-form'
import { isCompleteCep, maskCep } from '@/lib/cepInput'
import { maskPhone } from '@/lib/phoneInput'
import { cepService } from '@/models/services'
import type { Beneficiary, BeneficiaryAddress } from '@/models/types'
import { FormSection, type FormFieldConfig } from '@/views/components/form'

interface AddressFormValues {
  zipCode?: string
  street?: string
  number?: string
  complement?: string
  district?: string
  city?: string
  state?: string
}

export const emptyAddressValues = {
  zipCode: '',
  street: '',
  number: '',
  complement: '',
  district: '',
  city: '',
  state: '',
}

export function addressValuesOf(beneficiary: Beneficiary) {
  const address = beneficiary.address
  if (!address) return emptyAddressValues
  return {
    zipCode: maskCep(address.zipCode),
    street: address.street,
    number: address.number,
    complement: address.complement ?? '',
    district: address.district,
    city: address.city,
    state: address.state,
  }
}

export function contactValuesOf(beneficiary: Beneficiary) {
  return {
    email: beneficiary.email ?? '',
    phone: maskPhone(beneficiary.phones[0] ?? ''),
    secondaryPhone: maskPhone(beneficiary.phones[1] ?? ''),
  }
}

export function contactPhonesOf({
  phone,
  secondaryPhone,
}: {
  phone?: string
  secondaryPhone?: string
}): string[] {
  return [phone, secondaryPhone].filter((value): value is string => !!value)
}

export function addressInputOf(values: AddressFormValues): BeneficiaryAddress | null {
  const isBlank = Object.values(values).every((value) => !value?.trim())
  if (isBlank) return null
  return {
    zipCode: values.zipCode ?? '',
    street: values.street ?? '',
    number: values.number ?? '',
    complement: values.complement || null,
    district: values.district ?? '',
    city: values.city ?? '',
    state: values.state ?? '',
  }
}

function section(key: string, title: string): FormFieldConfig {
  return {
    type: 'custom',
    key,
    colSpan: 'full',
    render: () => <FormSection title={title} />,
  }
}

async function fillAddressFromCep(zipCode: string, form: AnyFormApi) {
  if (!isCompleteCep(zipCode)) return
  const found = await cepService.lookup(zipCode)
  if (!found) return
  const suggestions = {
    street: found.street,
    district: found.district,
    city: found.city,
    state: found.state,
  }
  for (const [field, value] of Object.entries(suggestions)) {
    if (value) form.setFieldValue(field as never, value as never)
  }
}

export const personalFields: FormFieldConfig[] = [
  section('section-personal', 'Dados pessoais'),
  { name: 'name', type: 'text', label: 'Nome', required: true, colSpan: 2 },
  {
    name: 'cpf',
    type: 'text',
    mask: 'cpf',
    label: 'CPF',
    placeholder: '000.000.000-00',
    required: true,
  },
  { name: 'birthDate', type: 'date', label: 'Nascimento', required: true },
]

export const contactFields: FormFieldConfig[] = [
  section('section-contact', 'Contato'),
  {
    name: 'email',
    type: 'email',
    label: 'E-mail',
    placeholder: 'nome@exemplo.com',
    autoComplete: 'off',
    required: true,
    colSpan: 2,
  },
  {
    name: 'phone',
    type: 'text',
    mask: 'phone',
    label: 'Telefone de contato',
    placeholder: '(11) 98765-4321',
    required: true,
  },
  {
    name: 'secondaryPhone',
    type: 'text',
    mask: 'phone',
    label: 'Segundo telefone',
    placeholder: '(11) 3456-7890',
  },
]

export const addressFields: FormFieldConfig[] = [
  section('section-address', 'Endereço'),
  {
    name: 'zipCode',
    type: 'text',
    mask: 'cep',
    label: 'CEP',
    placeholder: '00000-000',
    autoComplete: 'off',
    onValueChange: fillAddressFromCep,
  },
  { name: 'street', type: 'text', label: 'Rua', colSpan: 2 },
  { name: 'number', type: 'text', label: 'Número', maxLength: 20 },
  { name: 'complement', type: 'text', label: 'Complemento', placeholder: 'Apto, bloco…' },
  { name: 'district', type: 'text', label: 'Bairro' },
  { name: 'city', type: 'text', label: 'Cidade' },
  { name: 'state', type: 'text', label: 'UF', maxLength: 2, placeholder: 'SC' },
]

export function registrationFields(
  pointOptions: { value: string; label: string }[],
): FormFieldConfig[] {
  return [
    section('section-registration', 'Cadastro'),
    {
      name: 'registeredByPointId',
      type: 'autocomplete',
      label: 'Ponto de cadastro',
      options: pointOptions,
      placeholder: 'Selecione…',
      searchPlaceholder: 'Buscar ponto…',
      emptyText: 'Nenhum ponto encontrado.',
      required: true,
      colSpan: 2,
    },
  ]
}
