const API = import.meta.env.VITE_API_URL || 'http://localhost:5001'

export async function fetchInventory() {
  const res = await fetch(`${API}/api/inventory`)
  if (!res.ok) throw new Error('Failed to fetch inventory')
  return res.json()
}

export async function fetchActiveHolds() {
  const res = await fetch(`${API}/api/holds`)
  if (!res.ok) throw new Error('Failed to fetch active holds')
  const data = await res.json()
  return data.map((h: any) => ({
    id: h.holdId,
    sku: h.sku,
    quantity: h.quantity,
    expiresAt: h.expiresAt
  }))
}

export async function createHold(payload: any) {
  const res = await fetch(`${API}/api/holds`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  })
  if (!res.ok) {
    const err = await res.json().catch(() => null)
    throw err || new Error('Failed to create hold')
  }
  const h = await res.json()
  return {
    id: h.holdId,
    sku: h.sku,
    quantity: h.quantity,
    expiresAt: h.expiresAt
  }
}

export async function releaseHold(id: string) {
  const res = await fetch(`${API}/api/holds/${encodeURIComponent(id)}`, { method: 'DELETE' })
  if (!res.ok) {
    const err = await res.json().catch(() => null)
    throw err || new Error('Failed to release hold')
  }
  return true
}
