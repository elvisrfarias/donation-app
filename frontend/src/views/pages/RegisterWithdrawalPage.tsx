import { zodResolver } from '@hookform/resolvers/zod'
import { CheckCircle2, PackageCheck, Search } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import {
  registerWithdrawalSchema,
  useCollectionPoints,
  useFamily,
  useRegisterWithdrawal,
  type RegisterWithdrawalForm,
} from '@/viewmodels'
import {
  Alert,
  Badge,
  Button,
  Card,
  FormField,
  Input,
  SectionTitle,
  Select,
  Textarea,
} from '@/views/components/ui'

export function RegisterWithdrawalPage() {
  const { points } = useCollectionPoints()
  const withdrawal = useRegisterWithdrawal()

  const {
    register,
    handleSubmit,
    getValues,
    reset,
    setValue,
    formState: { errors },
  } = useForm<RegisterWithdrawalForm>({
    resolver: zodResolver(registerWithdrawalSchema),
    defaultValues: { pointId: '', familyId: '', beneficiaryId: '', notes: '' },
  })

  const [loadedFamilyId, setLoadedFamilyId] = useState<string | null>(null)
  const family = useFamily(loadedFamilyId)

  const loadFamily = () => {
    setLoadedFamilyId(getValues('familyId').trim() || null)
    setValue('beneficiaryId', '')
  }

  const onSubmit = handleSubmit(async (values) => {
    try {
      await withdrawal.register({
        pointId: values.pointId,
        familyId: values.familyId,
        beneficiaryId: values.beneficiaryId,
        notes: values.notes || null,
      })
      reset({ pointId: values.pointId, familyId: values.familyId, beneficiaryId: '', notes: '' })
    } catch {
      /* erro exibido via withdrawal.error */
    }
  })

  const beneficiaries = family.family?.beneficiaries ?? []
  const remaining = family.history?.remainingInCurrentCycle
  const limitReached = remaining === 0

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <SectionTitle
        icon={<PackageCheck className="size-5" />}
        title="Registrar Retirada"
        description="A regra de equidade é validada pelo backend, somando todos os pontos do município."
      />

      <Card className="p-6">
        {withdrawal.lastWithdrawal && (
          <Alert tone="success" className="mb-4" title="Retirada registrada!">
            Registrada em {new Date(withdrawal.lastWithdrawal.occurredAtUtc).toLocaleString('pt-BR')}.
          </Alert>
        )}
        {withdrawal.error && (
          <Alert
            tone={withdrawal.error.isConflict ? 'warning' : 'error'}
            className="mb-4"
            title={withdrawal.error.isConflict ? 'Não permitido' : undefined}
          >
            {withdrawal.error.displayMessage}
          </Alert>
        )}

        <form onSubmit={onSubmit} className="space-y-4" noValidate>
          <FormField label="Ponto de coleta" htmlFor="pointId" error={errors.pointId?.message} required>
            <Select id="pointId" {...register('pointId')} invalid={!!errors.pointId}>
              <option value="">Selecione o ponto…</option>
              {points.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} — {p.address.city}/{p.address.state}
                </option>
              ))}
            </Select>
          </FormField>

          <FormField
            label="ID da família"
            htmlFor="familyId"
            error={errors.familyId?.message}
            hint="Informe o GUID da família e clique em carregar para escolher o beneficiário."
            required
          >
            <div className="flex gap-2">
              <Input
                id="familyId"
                className="font-mono text-xs"
                placeholder="GUID da família"
                {...register('familyId')}
                invalid={!!errors.familyId}
              />
              <Button
                type="button"
                variant="secondary"
                leftIcon={<Search className="size-4" />}
                loading={family.isFetching}
                onClick={loadFamily}
              >
                Carregar
              </Button>
            </div>
          </FormField>

          {loadedFamilyId && family.notFound && (
            <Alert tone="warning">Família não encontrada para o ID informado.</Alert>
          )}

          {family.family && (
            <div className="rounded-xl border border-stone-200 bg-stone-50 p-3.5">
              <div className="flex items-center justify-between">
                <p className="text-sm font-semibold text-stone-700">{family.family.name}</p>
                {remaining !== undefined && (
                  <Badge tone={limitReached ? 'red' : 'green'}>
                    {limitReached
                      ? 'Limite do ciclo atingido'
                      : `${remaining} retirada(s) restante(s)`}
                  </Badge>
                )}
              </div>
            </div>
          )}

          <FormField
            label="Beneficiário"
            htmlFor="beneficiaryId"
            error={errors.beneficiaryId?.message}
            required
          >
            <Select
              id="beneficiaryId"
              {...register('beneficiaryId')}
              invalid={!!errors.beneficiaryId}
              disabled={!family.family}
            >
              <option value="">
                {family.family ? 'Selecione o beneficiário…' : 'Carregue a família primeiro'}
              </option>
              {beneficiaries.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name} ({b.cpf})
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Observações" htmlFor="notes" error={errors.notes?.message}>
            <Textarea
              id="notes"
              placeholder="Itens retirados, observações…"
              {...register('notes')}
              invalid={!!errors.notes}
            />
          </FormField>

          <Button
            type="submit"
            size="lg"
            loading={withdrawal.isPending}
            leftIcon={<CheckCircle2 className="size-5" />}
            disabled={limitReached}
          >
            Registrar retirada
          </Button>
        </form>
      </Card>
    </div>
  )
}
