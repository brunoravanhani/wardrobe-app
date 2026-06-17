import { describe, expect, it } from 'vitest'
import {
  CLOTHING_CATEGORIES,
  coerceCategoryString,
  getCategoryLabelPtBr,
} from '../../../src/services/wardrobeApi'

describe('clothing categories', () => {
  it('includes the Polo and Accessories categories', () => {
    expect(CLOTHING_CATEGORIES).toContain('Polo')
    expect(CLOTHING_CATEGORIES).toContain('Accessories')
  })

  it('maps the new categories to pt-BR labels', () => {
    expect(getCategoryLabelPtBr('Polo')).toBe('Polo')
    expect(getCategoryLabelPtBr('Accessories')).toBe('Acessórios')
  })

  it('coerces the new numeric category codes', () => {
    expect(coerceCategoryString(8)).toBe('Polo')
    expect(coerceCategoryString(9)).toBe('Accessories')
  })
})
