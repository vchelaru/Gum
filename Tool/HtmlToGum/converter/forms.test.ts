// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { formControlFromDom, formsBaseType, treeHasFormControls, emitFormsControlVars } from './forms.js';
import { mapTreeToScreen } from './map.js';

test('formsBaseType maps HTML roles to Controls/*', () => {
  assert.equal(formsBaseType({ role: 'textbox' }), 'Controls/TextBox');
  assert.equal(formsBaseType({ role: 'password' }), 'Controls/PasswordBox');
  assert.equal(formsBaseType({ role: 'button' }), 'Controls/ButtonStandard');
  assert.equal(formsBaseType({ role: 'submit' }), 'Controls/ButtonStandard');
  assert.equal(formsBaseType({ role: 'checkbox' }), 'Controls/CheckBox');
  assert.equal(formsBaseType({ role: 'radio' }), 'Controls/RadioButton');
  assert.equal(formsBaseType({ role: 'combobox' }), 'Controls/ComboBox');
  assert.equal(formsBaseType(null), null);
});

test('formControlFromDom: input types', () => {
  const email = formControlFromDom({
    tagName: 'INPUT', type: 'email', placeholder: 'Email', value: '', checked: false, disabled: false,
  });
  assert.deepEqual(email, {
    role: 'textbox', inputType: 'email', placeholder: 'Email', value: '', checked: false, disabled: false,
  });

  const pw = formControlFromDom({
    tagName: 'INPUT', type: 'password', placeholder: 'Password', value: 'x', checked: false, disabled: false,
  });
  assert.equal(pw.role, 'password');

  const submit = formControlFromDom({
    tagName: 'INPUT', type: 'submit', placeholder: '', value: 'Sign In', checked: false, disabled: false,
  });
  assert.equal(submit.role, 'submit');
  assert.equal(submit.value, 'Sign In');

  assert.equal(formControlFromDom({
    tagName: 'INPUT', type: 'hidden', placeholder: '', value: '1', checked: false, disabled: false,
  }), null);
});

test('formControlFromDom: button / textarea / select', () => {
  assert.equal(formControlFromDom({
    tagName: 'BUTTON', type: 'button', innerText: 'Cancel', textContent: 'Cancel', disabled: false,
  }).role, 'button');

  assert.equal(formControlFromDom({
    tagName: 'TEXTAREA', placeholder: 'Notes', value: 'Hi', disabled: false,
  }).role, 'textbox');

  const sel = formControlFromDom({
    tagName: 'SELECT',
    disabled: false,
    options: [{ text: 'A', value: 'a' }, { text: 'B', value: 'b' }],
    selectedOptions: [{ text: 'B', value: 'b' }],
  });
  assert.equal(sel.role, 'combobox');
  assert.equal(sel.value, 'B');
});

test('treeHasFormControls walks the box tree', () => {
  assert.equal(treeHasFormControls({ children: [] }), false);
  assert.equal(treeHasFormControls({
    children: [{ form: { role: 'textbox' }, children: [] }],
  }), true);
});

test('mapTreeToScreen emits Forms BaseTypes when formsEnabled', () => {
  const root = {
    id: 'panel',
    tag: 'form',
    rect: { x: 0, y: 0, width: 300, height: 200 },
    text: '',
    lineCount: 1,
    imgSrc: null,
    naturalWidth: 0,
    naturalHeight: 0,
    rasterSrc: null,
    style: {
      display: 'block', backgroundImage: 'none', backgroundSize: 'auto',
      backgroundPosition: '0% 0%', backgroundRepeat: 'repeat', objectFit: 'fill',
      objectPosition: '50% 50%', listStyleType: 'none', flexDirection: 'column',
      flexWrap: 'nowrap', rowGap: 0, columnGap: 0, flexGrow: 0, order: 0,
      alignItems: 'stretch', alignSelf: 'auto', justifyContent: 'normal',
      textAlign: 'left', paddingTop: 0, paddingRight: 0, paddingBottom: 0, paddingLeft: 0,
      marginTop: 0, marginRight: 0, marginBottom: 0, marginLeft: 0, zIndex: 'auto',
      position: 'static', overflow: 'visible', opacity: '1', color: 'rgb(0,0,0)',
      fontFamily: 'Arial', fontSize: '14px', fontWeight: '400', fontStyle: 'normal',
      lineHeight: 'normal', whiteSpace: 'normal', textTransform: 'none',
      backgroundColor: 'rgb(255,255,255)', borderTopWidth: 0, borderRightWidth: 0,
      borderBottomWidth: 0, borderLeftWidth: 0, borderTopColor: 'rgb(0,0,0)',
      borderRightColor: 'rgb(0,0,0)', borderBottomColor: 'rgb(0,0,0)',
      borderLeftColor: 'rgb(0,0,0)', borderTopLeftRadius: 0, borderTopRightRadius: 0,
      borderBottomRightRadius: 0, borderBottomLeftRadius: 0, boxShadow: 'none',
      textShadow: 'none', filter: 'none', borderImageSource: 'none',
      borderImageSlice: 0, borderImageRepeat: 'stretch',
      widthSpecified: '', heightSpecified: '',
      needsRaster: false, rasterWholeSubtree: false, rasterOmitBackground: false,
    },
    children: [
      {
        id: 'email',
        tag: 'input',
        rect: { x: 10, y: 10, width: 280, height: 32 },
        text: '',
        lineCount: 1,
        imgSrc: null,
        naturalWidth: 0,
        naturalHeight: 0,
        rasterSrc: null,
        form: {
          role: 'textbox', inputType: 'email', placeholder: 'Email',
          value: '', checked: false, disabled: false,
        },
        style: {
          display: 'inline-block', backgroundImage: 'none', backgroundSize: 'auto',
          backgroundPosition: '0% 0%', backgroundRepeat: 'repeat', objectFit: 'fill',
          objectPosition: '50% 50%', listStyleType: 'none', flexDirection: 'row',
          flexWrap: 'nowrap', rowGap: 0, columnGap: 0, flexGrow: 0, order: 0,
          alignItems: 'normal', alignSelf: 'auto', justifyContent: 'normal',
          textAlign: 'left', paddingTop: 0, paddingRight: 0, paddingBottom: 0, paddingLeft: 0,
          marginTop: 0, marginRight: 0, marginBottom: 0, marginLeft: 0, zIndex: 'auto',
          position: 'static', overflow: 'visible', opacity: '1', color: 'rgb(0,0,0)',
          fontFamily: 'Arial', fontSize: '14px', fontWeight: '400', fontStyle: 'normal',
          lineHeight: 'normal', whiteSpace: 'normal', textTransform: 'none',
          backgroundColor: 'rgb(255,255,255)', borderTopWidth: 1, borderRightWidth: 1,
          borderBottomWidth: 1, borderLeftWidth: 1, borderTopColor: 'rgb(0,0,0)',
          borderRightColor: 'rgb(0,0,0)', borderBottomColor: 'rgb(0,0,0)',
          borderLeftColor: 'rgb(0,0,0)', borderTopLeftRadius: 0, borderTopRightRadius: 0,
          borderBottomRightRadius: 0, borderBottomLeftRadius: 0, boxShadow: 'none',
          textShadow: 'none', filter: 'none', borderImageSource: 'none',
          borderImageSlice: 0, borderImageRepeat: 'stretch',
          widthSpecified: '', heightSpecified: '',
          needsRaster: false, rasterWholeSubtree: false, rasterOmitBackground: false,
        },
        children: [],
      },
      {
        id: 'go',
        tag: 'input',
        rect: { x: 10, y: 50, width: 120, height: 32 },
        text: 'Sign In',
        lineCount: 1,
        imgSrc: null,
        naturalWidth: 0,
        naturalHeight: 0,
        rasterSrc: null,
        form: {
          role: 'submit', inputType: 'submit', placeholder: '',
          value: 'Sign In', checked: false, disabled: false,
        },
        style: {
          display: 'inline-block', backgroundImage: 'none', backgroundSize: 'auto',
          backgroundPosition: '0% 0%', backgroundRepeat: 'repeat', objectFit: 'fill',
          objectPosition: '50% 50%', listStyleType: 'none', flexDirection: 'row',
          flexWrap: 'nowrap', rowGap: 0, columnGap: 0, flexGrow: 0, order: 0,
          alignItems: 'normal', alignSelf: 'auto', justifyContent: 'normal',
          textAlign: 'center', paddingTop: 0, paddingRight: 0, paddingBottom: 0, paddingLeft: 0,
          marginTop: 0, marginRight: 0, marginBottom: 0, marginLeft: 0, zIndex: 'auto',
          position: 'static', overflow: 'visible', opacity: '1', color: 'rgb(255,255,255)',
          fontFamily: 'Arial', fontSize: '14px', fontWeight: '600', fontStyle: 'normal',
          lineHeight: 'normal', whiteSpace: 'normal', textTransform: 'none',
          backgroundColor: 'rgb(37,99,235)', borderTopWidth: 0, borderRightWidth: 0,
          borderBottomWidth: 0, borderLeftWidth: 0, borderTopColor: 'rgb(0,0,0)',
          borderRightColor: 'rgb(0,0,0)', borderBottomColor: 'rgb(0,0,0)',
          borderLeftColor: 'rgb(0,0,0)', borderTopLeftRadius: 0, borderTopRightRadius: 0,
          borderBottomRightRadius: 0, borderBottomLeftRadius: 0, boxShadow: 'none',
          textShadow: 'none', filter: 'none', borderImageSource: 'none',
          borderImageSlice: 0, borderImageRepeat: 'stretch',
          widthSpecified: '', heightSpecified: '',
          needsRaster: false, rasterWholeSubtree: false, rasterOmitBackground: false,
        },
        children: [],
      },
    ],
  };

  const withForms = mapTreeToScreen(root, new Map(), null, null, null, null, true);
  const types = withForms.instances.map((i) => i.baseType);
  assert.ok(types.includes('Controls/TextBox'), `expected TextBox, got ${types.join(',')}`);
  assert.ok(types.includes('Controls/ButtonStandard'), `expected ButtonStandard, got ${types.join(',')}`);
  assert.ok(withForms.variables.some((v) => v.name.endsWith('.Placeholder') && v.value === 'Email'));
  assert.ok(withForms.variables.some((v) => v.name.endsWith('.Text') && v.value === 'Sign In'));

  const noForms = mapTreeToScreen(root, new Map(), null, null, null, null, false);
  const chromeTypes = noForms.instances.map((i) => i.baseType);
  assert.ok(!chromeTypes.some((t) => t.startsWith('Controls/')),
    `expected no Controls/* with formsEnabled=false, got ${chromeTypes.join(',')}`);
});

test('emitFormsControlVars: checkbox IsChecked + Password', () => {
  const VS = (n, val) => ({ name: n, value: val });
  const VF = (n, val) => ({ name: n, value: val });
  const VDIM = (n, val) => ({ name: n, value: val });
  const VB = (n, val) => ({ name: n, value: val });
  const DIM = { Absolute: 0 };

  const pw = emitFormsControlVars(
    'Pw',
    {
      rect: { x: 5, y: 5, width: 100, height: 30 },
      form: { role: 'password', value: 'secret', placeholder: 'Password', disabled: false },
      text: '',
    },
    'Parent',
    { x: 0, y: 0 },
    VS, VF, VDIM, VB, DIM,
  );
  assert.ok(pw.some((v) => v.name === 'Pw.Password' && v.value === 'secret'));
  assert.ok(pw.some((v) => v.name === 'Pw.Placeholder' && v.value === 'Password'));

  const cb = emitFormsControlVars(
    'Cb',
    {
      rect: { x: 0, y: 0, width: 20, height: 20 },
      form: { role: 'checkbox', value: 'on', checked: true, disabled: false, placeholder: '' },
      text: 'Remember',
    },
    null,
    { x: 0, y: 0 },
    VS, VF, VDIM, VB, DIM,
  );
  assert.ok(cb.some((v) => v.name === 'Cb.IsChecked' && v.value === true));
  assert.ok(cb.some((v) => v.name === 'Cb.Text' && v.value === 'Remember'));
});
