import { zodResolver } from '@hookform/resolvers/zod'
import { CalendarClock, IdCard, Plus, Search, UserPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { formatDate, formatDateTime } from '@/lib/utils'
import { cycleTypeLabels } from '@/models/types'
import {
  createFamilySchema,
  registerBeneficiarySchema,
  useAuth,
  useCollectionPoints,
  useCreateFamily,
  useFamily,
  useRegisterBeneficiary,
  type CreateFamilyForm,
  type RegisterBeneficiaryForm,
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

export function FamiliesPage() {
  const { isManager } = useAuth()
  const { points } = useCollectionPoints()
  const createFamily = useCreateFamily()

  const [familyId, setFamilyId] = useState<string | null>(null)
  const [lookupInput, setLookupInput] = useState('')
  const family = useFamily(familyId)
  const registerBeneficiary = useRegisterBeneficiary(familyId ?? '')

  const familyForm = useForm<CreateFamilyForm>({
    resolver: zodResolver(createFamilySchema),
    defaultValues: { name: '', cycleType: 'Monthly', maxWithdrawalsPerCycle: 1 },
  })

  const beneficiaryForm = useForm<RegisterBeneficiaryForm>({
    resolver: zodResolver(registerBeneficiarySchema),
    defaultValues: { name: '', cpf: '', birthDate: '', registeredByPointId: '' },
  })

  const onCreateFamily = familyForm.handleSubmit(async (values) => {
    try {
      await createFamily.create(values)
      familyForm.reset({ name: '', cycleType: 'Monthly', maxWithdrawalsPerCycle: 1 })
    } catch {
      /* erro exibido via createFamily.error */
    }
  })

  const onRegisterBeneficiary = beneficiaryForm.handleSubmit(async (values) => {
    try {
      await registerBeneficiary.register({
        name: values.name,
        cpf: values.cpf,
        birthDate: values.birthDate || null,
        registeredByPointId: values.registeredByPointId,
      })
      beneficiaryForm.reset({ name: '', cpf: '', birthDate: '', registeredByPointId: '' })
    } catch {
      /* erro exibido via registerBeneficiary.error */
    }
  })

  const pointName = (id: string) => points.find((p) => p.id === id)?.name ?? `${id.slice(0, 8)}…`
  const beneficiaryName = (id: string) =>
    family.family?.beneficiaries.find((b) => b.id === id)?.name ?? `${id.slice(0, 8)}…`

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <SectionTitle
        icon={<Users className="size-5" />}
        title="Famílias"
        description="Cadastro, beneficiários e histórico municipal de retiradas."
      />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Criar família */}
        {isManager ? (
          <Card className="p-5">
            <h2 className="font-semibold text-slate-900">Nova família</h2>
            {createFamily.createdFamily && (
              <Alert tone="success" className="mt-3" title="Família criada!">
                ID: <code className="font-mono text-xs">{createFamily.createdFamily.id}</code>
              </Alert>
            )}
            {createFamily.error && (
              <Alert tone="error" className="mt-3">
                {createFamily.error.displayMessage}
              </Alert>
            )}
            <form onSubmit={onCreateFamily} className="mt-4 space-y-4" noValidate>
              <FormField
                label="Nome / referência"
                htmlFor="familyName"
                error={familyForm.formState.errors.name?.message}
                required
              >
                <Input
                  id="familyName"
                  placeholder="Família Silva"
                  {...familyForm.register('name')}
                  invalid={!!familyForm.formState.errors.name}
                />
              </FormField>
              <div className="grid grid-cols-2 gap-3">
                <FormField
                  label="Ciclo"
                  htmlFor="cycleType"
                  error={familyForm.formState.errors.cycleType?.message}
                  required
                >
                  <Select id="cycleType" {...familyForm.register('cycleType')}>
                    {Object.entries(cycleTypeLabels).map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </Select>
                </FormField>
                <FormField
                  label="Máx. por ciclo"
                  htmlFor="maxWithdrawals"
                  error={familyForm.formState.errors.maxWithdrawalsPerCycle?.message}
                  required
                >
                  <Input
                    id="maxWithdrawals"
                    type="number"
                    min={1}
                    {...familyForm.register('maxWithdrawalsPerCycle', { valueAsNumber: true })}
                    invalid={!!familyForm.formState.errors.maxWithdrawalsPerCycle}
                  />
                </FormField>
              </div>
              <Button type="submit" loading={createFamily.isPending} leftIcon={<Plus className="size-4" />}>
                Criar família
              </Button>
            </form>
          </Card>
        ) : (
          <Alert tone="info">Apenas gestores/administradores podem criar famílias.</Alert>
        )}

        {/* Consultar família */}
        <Card className="p-5">
          <h2 className="font-semibold text-slate-900">Consultar família</h2>
          <p className="mt-1 text-sm text-slate-500">Informe o ID da família para ver detalhes.</p>
          <form
            className="mt-4 flex gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              setFamilyId(lookupInput.trim() || null)
            }}
          >
            <Input
              value={lookupInput}
              onChange={(e) => setLookupInput(e.target.value)}
              placeholder="ID da família (GUID)"
              className="font-mono text-xs"
            />
            <Button type="submit" leftIcon={<Search className="size-4" />}>
              Carregar
            </Button>
          </form>
        </Card>
      </div>

      {/* Detalhes da família */}
      {familyId && (
        <>
          {family.isLoading ? (
            <CenteredSpinner label="Carregando família…" />
          ) : family.notFound ? (
            <Alert tone="warning" title="Família não encontrada">
              Verifique o ID informado.
            </Alert>
          ) : family.error ? (
            <Alert tone="error">{family.error.displayMessage}</Alert>
          ) : family.family ? (
            <div className="grid gap-4 lg:grid-cols-2">
              {/* Beneficiários */}
              <Card className="p-5">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold text-slate-900">{family.family.name}</h3>
                  <Badge tone="brand">
                    {cycleTypeLabels[family.family.cycle.type]} ·{' '}
                    {family.family.cycle.maxWithdrawalsPerCycle}/ciclo
                  </Badge>
                </div>

                <h4 className="mt-4 text-sm font-medium text-slate-500">Beneficiários</h4>
                {family.family.beneficiaries.length === 0 ? (
                  <p className="mt-2 text-sm text-slate-400">Nenhum beneficiário cadastrado.</p>
                ) : (
                  <ul className="mt-2 space-y-2">
                    {family.family.beneficiaries.map((b) => (
                      <li
                        key={b.id}
                        className="flex items-center justify-between rounded-lg border border-slate-200 p-2.5"
                      >
                        <div className="flex items-center gap-2">
                          <span className="grid size-8 place-items-center rounded-full bg-slate-100 text-slate-500">
                            <IdCard className="size-4" />
                          </span>
                          <div>
                            <p className="text-sm font-medium text-slate-800">{b.name}</p>
                            <p className="text-xs text-slate-500">{b.cpf}</p>
                          </div>
                        </div>
                        <Badge tone={b.isActive ? 'green' : 'slate'}>
                          {b.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      </li>
                    ))}
                  </ul>
                )}

                {/* Cadastrar beneficiário */}
                {isManager && (
                  <form
                    onSubmit={onRegisterBeneficiary}
                    className="mt-5 space-y-3 border-t border-slate-100 pt-4"
                    noValidate
                  >
                    <p className="text-sm font-medium text-slate-700">Adicionar beneficiário</p>
                    {registerBeneficiary.error && (
                      <Alert tone="error">{registerBeneficiary.error.displayMessage}</Alert>
                    )}
                    <div className="grid grid-cols-2 gap-3">
                      <FormField
                        label="Nome"
                        htmlFor="benName"
                        error={beneficiaryForm.formState.errors.name?.message}
                        required
                      >
                        <Input id="benName" {...beneficiaryForm.register('name')} invalid={!!beneficiaryForm.formState.errors.name} />
                      </FormField>
                      <FormField
                        label="CPF"
                        htmlFor="benCpf"
                        error={beneficiaryForm.formState.errors.cpf?.message}
                        required
                      >
                        <Input id="benCpf" placeholder="000.000.000-00" {...beneficiaryForm.register('cpf')} invalid={!!beneficiaryForm.formState.errors.cpf} />
                      </FormField>
                      <FormField
                        label="Nascimento"
                        htmlFor="benBirth"
                        error={beneficiaryForm.formState.errors.birthDate?.message}
                      >
                        <Input id="benBirth" type="date" {...beneficiaryForm.register('birthDate')} />
                      </FormField>
                      <FormField
                        label="Ponto de cadastro"
                        htmlFor="benPoint"
                        error={beneficiaryForm.formState.errors.registeredByPointId?.message}
                        required
                      >
                        <Select id="benPoint" {...beneficiaryForm.register('registeredByPointId')} invalid={!!beneficiaryForm.formState.errors.registeredByPointId}>
                          <option value="">Selecione…</option>
                          {points.map((p) => (
                            <option key={p.id} value={p.id}>
                              {p.name}
                            </option>
                          ))}
                        </Select>
                      </FormField>
                    </div>
                    <Button
                      type="submit"
                      size="sm"
                      loading={registerBeneficiary.isPending}
                      leftIcon={<UserPlus className="size-4" />}
                    >
                      Adicionar
                    </Button>
                  </form>
                )}
              </Card>

              {/* Histórico / saldo do ciclo */}
              <Card className="p-5">
                <h3 className="flex items-center gap-2 font-semibold text-slate-900">
                  <CalendarClock className="size-5 text-brand-600" /> Ciclo vigente
                </h3>
                {family.history ? (
                  <>
                    <div className="mt-3 grid grid-cols-3 gap-2 text-center">
                      <div className="rounded-lg bg-slate-50 p-3">
                        <p className="text-2xl font-bold text-slate-900">
                          {family.history.usedInCurrentCycle}
                        </p>
                        <p className="text-xs text-slate-500">Usadas</p>
                      </div>
                      <div className="rounded-lg bg-emerald-50 p-3">
                        <p className="text-2xl font-bold text-emerald-700">
                          {family.history.remainingInCurrentCycle}
                        </p>
                        <p className="text-xs text-emerald-600">Restantes</p>
                      </div>
                      <div className="rounded-lg bg-slate-50 p-3">
                        <p className="text-2xl font-bold text-slate-900">
                          {family.history.maxPerCycle}
                        </p>
                        <p className="text-xs text-slate-500">Limite</p>
                      </div>
                    </div>
                    <p className="mt-2 text-xs text-slate-500">
                      Janela: {formatDate(family.history.currentCycleStartUtc)} —{' '}
                      {formatDate(family.history.currentCycleEndUtc)}
                    </p>

                    <h4 className="mt-4 text-sm font-medium text-slate-500">
                      Histórico de retiradas
                    </h4>
                    {family.history.withdrawals.length === 0 ? (
                      <p className="mt-2 text-sm text-slate-400">Nenhuma retirada registrada.</p>
                    ) : (
                      <ul className="mt-2 divide-y divide-slate-100">
                        {family.history.withdrawals.map((w) => (
                          <li key={w.id} className="flex items-center justify-between py-2 text-sm">
                            <div>
                              <p className="font-medium text-slate-700">{beneficiaryName(w.beneficiaryId)}</p>
                              <p className="text-xs text-slate-500">{pointName(w.pointId)}</p>
                            </div>
                            <span className="text-xs text-slate-500">
                              {formatDateTime(w.occurredAtUtc)}
                            </span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </>
                ) : (
                  <EmptyState title="Sem histórico" description="Não foi possível carregar o histórico." />
                )}
              </Card>
            </div>
          ) : null}
        </>
      )}
    </div>
  )
}
