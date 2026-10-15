import { useEffect, useRef, useState } from 'react'
import { type AnyFormApi, useStore } from '@tanstack/react-form'
import { Check, MapPinned } from 'lucide-react'
import { cepService } from '@/models/services'
import type { Address, LatLng } from '@/models/types'
import { cn } from '@/lib/utils'
import { Button } from '@/views/components/ui'
import { LocationPickerModal, type PickedLocation } from './LocationPickerModal'

interface AddressValues {
  street?: string
  number?: string
  district?: string
  city?: string
  state?: string
  zipCode?: string
  latitude?: number
  longitude?: number
}

function onlyDigits(value: string | undefined): string {
  return (value ?? '').replace(/\D/g, '')
}

function buildAddress(values: AddressValues): Address | undefined {
  if (!values.city && !values.zipCode) return undefined
  return {
    street: values.street ?? '',
    number: values.number ?? '',
    complement: null,
    district: values.district ?? '',
    city: values.city ?? '',
    state: values.state ?? '',
    zipCode: values.zipCode ?? '',
  }
}

/**
 * Ferramentas de localização do endereço no formulário de ponto de coleta:
 * autopreenche os campos a partir do CEP (BrasilAPI) e abre um seletor de mapa
 * para fixar o ponto exato, capturando as coordenadas. O local exato é obrigatório: as
 * coordenadas só existem quando o usuário marca o pin no mapa, e alterar o CEP as zera para
 * exigir uma nova marcação.
 */
export function AddressLocationField({ form }: { form: AnyFormApi }) {
  const values = useStore(form.store, (s) => s.values as AddressValues)
  const [pickerOpen, setPickerOpen] = useState(false)
  const lastCepRef = useRef<string>(onlyDigits((form.state.values as AddressValues).zipCode))

  const submissionAttempts = useStore(form.store, (s) => s.submissionAttempts)
  const hasCoordinates = values.latitude != null && values.longitude != null
  const showMissingLocation = !hasCoordinates && submissionAttempts > 0

  useEffect(() => {
    const cep = onlyDigits(values.zipCode)
    if (cep.length !== 8 || cep === lastCepRef.current) return undefined
    lastCepRef.current = cep

    let cancelled = false
    cepService.lookup(cep).then((result) => {
      if (cancelled || !result) return
      if (result.street) form.setFieldValue('street', result.street)
      if (result.district) form.setFieldValue('district', result.district)
      if (result.city) form.setFieldValue('city', result.city)
      if (result.state) form.setFieldValue('state', result.state)
      form.setFieldValue('latitude', undefined)
      form.setFieldValue('longitude', undefined)
    })
    return () => {
      cancelled = true
    }
  }, [values.zipCode, form])

  function applyPicked({ position, address }: PickedLocation) {
    if (address.street) form.setFieldValue('street', address.street)
    if (address.number) form.setFieldValue('number', address.number)
    if (address.district) form.setFieldValue('district', address.district)
    if (address.city) form.setFieldValue('city', address.city)
    if (address.state) form.setFieldValue('state', address.state)
    if (address.zipCode) {
      lastCepRef.current = address.zipCode
      form.setFieldValue('zipCode', address.zipCode)
    }
    form.setFieldValue('latitude', position.lat)
    form.setFieldValue('longitude', position.lng)
  }

  const initialPosition: LatLng | null = hasCoordinates
    ? { lat: values.latitude as number, lng: values.longitude as number }
    : null

  return (
    <div
      className={cn(
        'flex flex-col gap-2 rounded-2xl border border-dashed p-3 sm:flex-row sm:items-center sm:justify-between',
        showMissingLocation ? 'border-red-300 bg-red-50' : 'border-stone-300 bg-stone-50/60',
      )}
    >
      <div
        className="flex items-center gap-2 text-sm"
        role={showMissingLocation ? 'alert' : undefined}
      >
        {hasCoordinates ? (
          <>
            <Check className="size-4 shrink-0 text-emerald-600" aria-hidden />
            <span className="text-stone-600">
              Local exato marcado no mapa
              <span className="ml-1 text-xs text-stone-500">
                ({(values.latitude as number).toFixed(5)}, {(values.longitude as number).toFixed(5)}
                )
              </span>
            </span>
          </>
        ) : (
          <span className={showMissingLocation ? 'font-medium text-red-700' : 'text-stone-600'}>
            Local exato no mapa <span className="text-red-600">*</span> — marque o pin do ponto para
            poder salvar.
          </span>
        )}
      </div>
      <Button
        type="button"
        variant={hasCoordinates ? 'secondary' : 'brand'}
        size="sm"
        leftIcon={<MapPinned className="size-4" />}
        onClick={() => setPickerOpen(true)}
      >
        {hasCoordinates ? 'Ajustar no mapa' : 'Marcar no mapa'}
      </Button>

      <LocationPickerModal
        open={pickerOpen}
        onOpenChange={setPickerOpen}
        initialPosition={initialPosition}
        initialAddress={buildAddress(values)}
        onConfirm={applyPicked}
      />
    </div>
  )
}
