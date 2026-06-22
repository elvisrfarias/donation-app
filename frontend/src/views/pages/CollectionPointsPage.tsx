import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Church, Landmark, MapPin, Plus } from 'lucide-react'
import { useForm } from 'react-hook-form'
import {
  collectionPointTypeLabels,
  formatAddress,
  type CollectionPointType,
} from '@/models/types'
import {
  createCollectionPointSchema,
  useAuth,
  useCollectionPoints,
  useCreateCollectionPoint,
  type CreateCollectionPointForm,
} from '@/viewmodels'
import {
  Alert,
  Badge,
  Button,
  Card,
  CenteredSpinner,
  EmptyState,
  FormField,
  Input,
  SectionTitle,
  Select,
} from '@/views/components/ui'

const typeIcon: Record<CollectionPointType, typeof Building2> = {
  Association: Building2,
  Church,
  PublicEntity: Landmark,
}

export function CollectionPointsPage() {
  const { isAdmin } = useAuth()
  const { points, isLoading, isError, error } = useCollectionPoints()
  const { create, isPending, error: createError, createdPoint } = useCreateCollectionPoint()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateCollectionPointForm>({
    resolver: zodResolver(createCollectionPointSchema),
    defaultValues: {
      name: '',
      type: 'Association',
      street: '',
      number: '',
      complement: '',
      district: '',
      city: '',
      state: '',
      zipCode: '',
      responsibleName: '',
      email: '',
      phone: '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await create(values)
      reset()
    } catch {
      /* erro exibido via createError */
    }
  })

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <SectionTitle
        icon={<MapPin className="size-5" />}
        title="Pontos de Coleta"
        description="Locais cadastrados que compartilham o histórico de retiradas do município."
      />

      {isAdmin ? (
        <Card className="p-5">
          <h2 className="font-semibold text-slate-900">Cadastrar novo ponto</h2>
          {createdPoint && (
            <Alert tone="success" className="mt-3" title="Ponto criado!">
              {createdPoint.name} foi cadastrado com sucesso.
            </Alert>
          )}
          {createError && (
            <Alert tone="error" className="mt-3">
              {createError.displayMessage}
            </Alert>
          )}

          <form onSubmit={onSubmit} className="mt-4 grid gap-4 sm:grid-cols-2" noValidate>
            <FormField
              label="Nome"
              htmlFor="name"
              error={errors.name?.message}
              required
              className="sm:col-span-2"
            >
              <Input id="name" {...register('name')} invalid={!!errors.name} />
            </FormField>

            <FormField label="Tipo" htmlFor="type" error={errors.type?.message} required>
              <Select id="type" {...register('type')} invalid={!!errors.type}>
                {Object.entries(collectionPointTypeLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="CEP" htmlFor="zipCode" error={errors.zipCode?.message} required>
              <Input id="zipCode" placeholder="00000000" {...register('zipCode')} invalid={!!errors.zipCode} />
            </FormField>

            <FormField
              label="Rua"
              htmlFor="street"
              error={errors.street?.message}
              required
              className="sm:col-span-2"
            >
              <Input id="street" {...register('street')} invalid={!!errors.street} />
            </FormField>

            <FormField label="Número" htmlFor="number" error={errors.number?.message} required>
              <Input id="number" {...register('number')} invalid={!!errors.number} />
            </FormField>

            <FormField label="Complemento" htmlFor="complement" error={errors.complement?.message}>
              <Input id="complement" {...register('complement')} invalid={!!errors.complement} />
            </FormField>

            <FormField label="Bairro" htmlFor="district" error={errors.district?.message} required>
              <Input id="district" {...register('district')} invalid={!!errors.district} />
            </FormField>

            <FormField label="Cidade" htmlFor="city" error={errors.city?.message} required>
              <Input id="city" {...register('city')} invalid={!!errors.city} />
            </FormField>

            <FormField label="UF" htmlFor="state" error={errors.state?.message} required>
              <Input id="state" maxLength={2} placeholder="SP" {...register('state')} invalid={!!errors.state} />
            </FormField>

            <FormField
              label="Responsável"
              htmlFor="responsibleName"
              error={errors.responsibleName?.message}
              required
            >
              <Input
                id="responsibleName"
                {...register('responsibleName')}
                invalid={!!errors.responsibleName}
              />
            </FormField>

            <FormField label="E-mail de contato" htmlFor="email" error={errors.email?.message} required>
              <Input id="email" type="email" {...register('email')} invalid={!!errors.email} />
            </FormField>

            <FormField label="Telefone" htmlFor="phone" error={errors.phone?.message} required>
              <Input id="phone" placeholder="11987654321" {...register('phone')} invalid={!!errors.phone} />
            </FormField>

            <Button
              type="submit"
              loading={isPending}
              leftIcon={<Plus className="size-4" />}
              className="justify-self-start sm:col-span-2"
            >
              Cadastrar ponto
            </Button>
          </form>
        </Card>
      ) : (
        <Alert tone="info">Apenas administradores podem cadastrar novos pontos de coleta.</Alert>
      )}

      {/* Lista */}
      {isError && <Alert tone="error">{error?.displayMessage}</Alert>}

      {isLoading ? (
        <CenteredSpinner label="Carregando pontos…" />
      ) : points.length === 0 ? (
        <EmptyState
          icon={<MapPin className="size-6" />}
          title="Nenhum ponto cadastrado"
          description="Os pontos cadastrados aparecerão aqui."
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {points.map((point) => {
            const Icon = typeIcon[point.type]
            return (
              <Card key={point.id} className="flex flex-col gap-3 p-4">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2">
                    <span className="grid size-9 place-items-center rounded-lg bg-brand-50 text-brand-600">
                      <Icon className="size-5" />
                    </span>
                    <div>
                      <p className="font-semibold text-slate-900">{point.name}</p>
                      <p className="text-xs text-slate-500">
                        {collectionPointTypeLabels[point.type]}
                      </p>
                    </div>
                  </div>
                  <Badge tone={point.isActive ? 'green' : 'slate'}>
                    {point.isActive ? 'Ativo' : 'Inativo'}
                  </Badge>
                </div>
                <p className="text-sm text-slate-600">{formatAddress(point.address)}</p>
                <div className="mt-auto border-t border-slate-100 pt-2 text-xs text-slate-500">
                  <p className="font-medium text-slate-600">{point.contact.responsibleName}</p>
                  <p>
                    {point.contact.phone} · {point.contact.email}
                  </p>
                </div>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
