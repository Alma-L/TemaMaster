/**
 * Shared Albanian display labels for the raw UCI Bank Marketing category
 * codes ("management", "cellular", "poutcome=success", ...) used by both
 * the New Decision form and the client profile view, so they never drift
 * out of sync with each other.
 */

export interface LabelOption {
  value: string;
  label: string;
}

export const JOBS: LabelOption[] = [
  { value: 'admin.', label: 'Nëpunës/e (Administratë)' },
  { value: 'blue-collar', label: 'Punëtor/e' },
  { value: 'entrepreneur', label: 'Sipërmarrës/e' },
  { value: 'housemaid', label: 'Shtëpiake' },
  { value: 'management', label: 'Menaxhim' },
  { value: 'retired', label: 'I/e Pensionuar' },
  { value: 'self-employed', label: 'I/e Vetëpunësuar' },
  { value: 'services', label: 'Shërbime' },
  { value: 'student', label: 'Student/e' },
  { value: 'technician', label: 'Teknik/e' },
  { value: 'unemployed', label: 'I/e Papunë' },
  { value: 'unknown', label: 'Tjetër / Panjohur' },
];

export const MARITAL_OPTIONS: LabelOption[] = [
  { value: 'single', label: 'Beqar/e' },
  { value: 'married', label: 'I/e Martuar' },
  { value: 'divorced', label: 'I/e Divorcuar' },
];

export const EDUCATION_OPTIONS: LabelOption[] = [
  { value: 'primary', label: 'Fillore' },
  { value: 'secondary', label: 'E Mesme' },
  { value: 'tertiary', label: 'Universitare' },
  { value: 'unknown', label: 'Panjohur' },
];

export const YES_NO: LabelOption[] = [
  { value: 'no', label: 'Jo' },
  { value: 'yes', label: 'Po' },
];

export const CONTACT_OPTIONS: LabelOption[] = [
  { value: 'cellular', label: 'Celular' },
  { value: 'telephone', label: 'Telefon Fiks' },
  { value: 'unknown', label: 'Panjohur' },
];

export const MONTH_OPTIONS: LabelOption[] = [
  { value: 'jan', label: 'Janar' },
  { value: 'feb', label: 'Shkurt' },
  { value: 'mar', label: 'Mars' },
  { value: 'apr', label: 'Prill' },
  { value: 'may', label: 'Maj' },
  { value: 'jun', label: 'Qershor' },
  { value: 'jul', label: 'Korrik' },
  { value: 'aug', label: 'Gusht' },
  { value: 'sep', label: 'Shtator' },
  { value: 'oct', label: 'Tetor' },
  { value: 'nov', label: 'Nëntor' },
  { value: 'dec', label: 'Dhjetor' },
];

export const POUTCOME_OPTIONS: LabelOption[] = [
  { value: 'unknown', label: 'Pa Kontakt të Mëparshëm' },
  { value: 'failure', label: 'Dështim' },
  { value: 'other', label: 'Tjetër' },
  { value: 'success', label: 'Sukses' },
];

/** Look up the Albanian label for a raw category value; falls back to the raw value itself. */
export const labelFor = (options: LabelOption[], value: string): string =>
  options.find(o => o.value.toLowerCase() === (value ?? '').toLowerCase())?.label ?? value;
