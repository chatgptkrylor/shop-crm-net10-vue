export function statusBadgeClass(status: string): string {
  if (status === 'Lead') return 'badge badge-warning'
  if (status === 'Contact') return 'badge badge-info'
  return 'badge badge-success'
}

export function formatMmmDd(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString('en-US', { month: 'short', day: '2-digit' })
}

export function formatMmmDdYyyy(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString('en-US', { month: 'short', day: '2-digit', year: 'numeric' })
}

export function formatMmmDdYyyyHm(iso: string): string {
  const d = new Date(iso)
  const date = d.toLocaleString('en-US', { month: 'short', day: '2-digit', year: 'numeric' })
  const hh = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  return `${date} ${hh}:${mm}`
}
