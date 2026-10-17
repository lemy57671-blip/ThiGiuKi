/* Behaviors for the original Stitch instructor screens, backed by MVC forms. */
(function ($) {
  'use strict';
  function filter() {
    const text = ($('#exam-search').val() || '').toString().toLocaleLowerCase('vi');
    const status = $('#exam-status').val();
    let visible = 0;
    $('#exams-table tbody tr').each(function () {
      const show = (!status || this.dataset.status === status) && this.textContent.toLocaleLowerCase('vi').includes(text);
      this.hidden = !show;
      if (show) visible++;
    });
    $('#no-matches').prop('hidden', visible > 0);
  }
  $('#exam-search').on('input', filter);
  $('#exam-status').on('change', filter);
  $('#word-file').on('change', function () { $('#file-name').text(this.files[0] ? this.files[0].name : ''); });
  const zone = document.querySelector('.dropzone');
  if (zone) {
    zone.addEventListener('dragover', e => e.preventDefault());
    zone.addEventListener('drop', e => { e.preventDefault(); if (e.dataTransfer.files.length) { const input = document.querySelector('#word-file'); input.files=e.dataTransfer.files; input.dispatchEvent(new Event('change')); } });
  }
  $('#word-upload-form').on('submit', function () { $('#upload-submit').prop('disabled',true); $('#upload-progress').prop('hidden',false); });
  $('.exam-lifecycle-form').on('submit', function (event) {
    const exam = this.dataset.examTitle + ' (v' + this.dataset.examVersion + ')';
    const message = this.dataset.examAction === 'delete'
      ? 'Xóa bản nháp ' + exam + '? Toàn bộ câu hỏi và ảnh của bản nháp sẽ bị xóa. Bạn có thể nhập lại từ file Word.'
      : 'Lưu trữ đề ' + exam + '? Sinh viên không thể bắt đầu lượt mới. Bài đang làm và kết quả cũ vẫn được giữ.';
    if (!window.confirm(message)) { event.preventDefault(); return; }
    $(this).find('button[type=submit]').prop('disabled', true);
  });
  $('#open-publish').on('click', () => document.querySelector('#publish-dialog').showModal());
  $('#cancel-publish').on('click', () => document.querySelector('#publish-dialog').close());
  $('#question-edit-form input:not([type=hidden]):not([name=ConfirmAnswer]), #question-edit-form textarea, #question-edit-form select').on('input change', () => $('#question-edit-form [name=ConfirmAnswer]').prop('checked',false));
  $('a.image-link').on('click', function (event) {
    event.preventDefault();
    const dialog = document.createElement('dialog');
    dialog.dataset.readingImage='true';
    dialog.setAttribute('aria-label','Xem ảnh bài đọc');
    Object.assign(dialog.style,{width:'min(96vw,1200px)',maxWidth:'96vw',maxHeight:'94vh',overflow:'auto'});
    const close=document.createElement('button');close.type='button';close.className='secondary';close.textContent='Đóng ảnh';close.addEventListener('click',()=>dialog.close());
    const img=document.createElement('img');img.src=this.href;img.alt=this.querySelector('img').alt;img.style.width='100%';img.style.height='auto';
    dialog.append(close,img);document.body.append(dialog);dialog.addEventListener('close',()=>dialog.remove(),{once:true});dialog.showModal();
  });
})(jQuery);
