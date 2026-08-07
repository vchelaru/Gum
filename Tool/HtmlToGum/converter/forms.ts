// @ts-nocheck
// HTML form controls → Gum Forms component BaseTypes (Controls/TextBox, ButtonStandard, …).
// Visual-only convert used Rectangle/Text chrome; this path emits interactive Forms
// instances when the project was bootstrapped with `gumcli new --template forms`.

/**
 * @typedef {{
 *   role: 'textbox' | 'password' | 'button' | 'checkbox' | 'radio' | 'combobox' | 'submit',
 *   inputType: string,
 *   placeholder: string,
 *   value: string,
 *   checked: boolean,
 *   disabled: boolean,
 *   options?: string[],
 * }} FormControlInfo
 */

const TEXT_INPUT_TYPES = new Set([
  'text', 'email', 'search', 'tel', 'url', 'number', 'date', 'datetime-local',
  'month', 'week', 'time', '',
]);

/** @returns {FormControlInfo | null} */
export function formControlFromDom(el) {
  const tag = String(el.tagName || '').toUpperCase();
  if (tag === 'INPUT') {
    const inputType = String(el.type || 'text').toLowerCase();
    const placeholder = String(el.placeholder || '');
    const value = String(el.value || '');
    const checked = !!el.checked;
    const disabled = !!el.disabled;
    if (inputType === 'hidden' || inputType === 'file' || inputType === 'image'
      || inputType === 'range' || inputType === 'color' || inputType === 'reset') {
      return null; // not mapped yet / non-visual
    }
    if (inputType === 'password') {
      return { role: 'password', inputType, placeholder, value, checked, disabled };
    }
    if (inputType === 'checkbox') {
      return { role: 'checkbox', inputType, placeholder, value, checked, disabled };
    }
    if (inputType === 'radio') {
      return { role: 'radio', inputType, placeholder, value, checked, disabled };
    }
    if (inputType === 'submit' || inputType === 'button') {
      return {
        role: inputType === 'submit' ? 'submit' : 'button',
        inputType,
        placeholder,
        value: value || (inputType === 'submit' ? 'Submit' : 'Button'),
        checked,
        disabled,
      };
    }
    if (TEXT_INPUT_TYPES.has(inputType)) {
      return { role: 'textbox', inputType: inputType || 'text', placeholder, value, checked, disabled };
    }
    return null;
  }
  if (tag === 'TEXTAREA') {
    return {
      role: 'textbox',
      inputType: 'textarea',
      placeholder: String(el.placeholder || ''),
      value: String(el.value || ''),
      checked: false,
      disabled: !!el.disabled,
    };
  }
  if (tag === 'BUTTON') {
    const type = String(el.type || 'submit').toLowerCase();
    const label = String(el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim()
      || (type === 'submit' ? 'Submit' : 'Button');
    return {
      role: type === 'submit' ? 'submit' : 'button',
      inputType: type,
      placeholder: '',
      value: label,
      checked: false,
      disabled: !!el.disabled,
    };
  }
  if (tag === 'SELECT') {
    const options = Array.from(el.options || []).map((o) => String(o.text || o.value || '').trim());
    const selected = el.selectedOptions?.[0];
    const value = selected
      ? String(selected.text || selected.value || '').trim()
      : (options[0] || '');
    return {
      role: 'combobox',
      inputType: 'select',
      placeholder: '',
      value,
      checked: false,
      disabled: !!el.disabled,
      options,
    };
  }
  return null;
}

/** Gum component BaseType for a FormControlInfo, or null if unmapped. */
export function formsBaseType(form) {
  if (!form) return null;
  switch (form.role) {
    case 'textbox': return 'Controls/TextBox';
    case 'password': return 'Controls/PasswordBox';
    case 'button':
    case 'submit': return 'Controls/ButtonStandard';
    case 'checkbox': return 'Controls/CheckBox';
    case 'radio': return 'Controls/RadioButton';
    case 'combobox': return 'Controls/ComboBox';
    default: return null;
  }
}

export function treeHasFormControls(node) {
  if (!node) return false;
  if (node.form && formsBaseType(node.form)) return true;
  for (const c of node.children || []) {
    if (treeHasFormControls(c)) return true;
  }
  return false;
}

/**
 * Emit Parent/X/Y/Width/Height Absolute placement for a Forms control instance,
 * plus Text / Placeholder / IsChecked from the HTML form metadata.
 */
export function emitFormsControlVars(name, node, parentName, parentRect, VS, VF, VDIM, VB, DIM) {
  /** @type {object[]} */
  const variables = [];
  if (parentName) {
    variables.push(VS(`${name}.Parent`, parentName));
    variables.push(
      VF(`${name}.X`, Math.round(node.rect.x - parentRect.x)),
      VF(`${name}.Y`, Math.round(node.rect.y - parentRect.y)),
    );
  } else {
    variables.push(
      VF(`${name}.X`, Math.round(node.rect.x)),
      VF(`${name}.Y`, Math.round(node.rect.y)),
    );
  }
  variables.push(
    VDIM(`${name}.WidthUnits`, DIM.Absolute),
    VF(`${name}.Width`, Math.round(node.rect.width)),
    VDIM(`${name}.HeightUnits`, DIM.Absolute),
    VF(`${name}.Height`, Math.round(node.rect.height)),
  );

  const form = node.form;
  if (!form) return variables;

  if (form.role === 'textbox') {
    if (form.value) variables.push(VS(`${name}.Text`, form.value));
    if (form.placeholder) variables.push(VS(`${name}.Placeholder`, form.placeholder));
  } else if (form.role === 'password') {
    // PasswordBox uses Password (not Text); Placeholder is a FormsProperty.
    if (form.value) variables.push(VS(`${name}.Password`, form.value));
    if (form.placeholder) variables.push(VS(`${name}.Placeholder`, form.placeholder));
  } else if (form.role === 'button' || form.role === 'submit') {
    variables.push(VS(`${name}.Text`, form.value || 'Button'));
  } else if (form.role === 'checkbox' || form.role === 'radio') {
    // Prefer visible label text when the extract put it on the node; else value attr.
    const label = (node.text && node.text.trim()) || form.value || '';
    if (label) variables.push(VS(`${name}.Text`, label));
    variables.push(VB(`${name}.IsChecked`, !!form.checked));
  } else if (form.role === 'combobox') {
    if (form.value) variables.push(VS(`${name}.Text`, form.value));
  }

  if (form.disabled) {
    // Forms visuals use category states; IsEnabled is on FrameworkElement.
    variables.push(VB(`${name}.IsEnabled`, false));
  }
  return variables;
}
