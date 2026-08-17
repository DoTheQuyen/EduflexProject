const RICH_TEXT_TOOLBAR = [
  [{ header: [1, 2, 3, false] }],
  ['bold', 'italic', 'underline', 'strike'],
  [{ font: [] }],
  [{ size: ['small', false, 'large', 'huge'] }],
  [{ color: [] }, { background: [] }],
  [{ align: [] }],
  [{ list: 'ordered' }, { list: 'bullet' }],
  ['blockquote'],
  ['link'],
  ['clean'],
];

export const RICH_TEXT_QUILL_MODULES = { toolbar: RICH_TEXT_TOOLBAR };
