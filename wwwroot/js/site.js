// ===== Tiện ích chung =====
function showToast(msg, ok) {
    var el = document.getElementById('appToast');
    el.className = 'toast align-items-center border-0 ' + (ok === false ? 'text-bg-danger' : 'text-bg-success');
    $('#toastMsg').text(msg);
    bootstrap.Toast.getOrCreateInstance(el, { delay: 3000 }).show();
}

// ===== Danh sách lọc + phân trang bằng AJAX =====
var AjaxList = {
    load: function ($box, page) {
        page = page || 1;
        $box.data('page', page);
        var data = $($box.data('form')).serializeArray();
        data.push({ name: 'page', value: page });
        $.get($box.data('url'), data, function (html) { $box.find('.list-container').first().html(html); });
    },
    reload: function ($box) { AjaxList.load($box, $box.data('page') || 1); }
};

$(function () {
    // Gửi token CSRF kèm mọi yêu cầu AJAX
    var token = $('#afForm input[name="__RequestVerificationToken"]').val();
    $.ajaxSetup({ cache: false, headers: { 'RequestVerificationToken': token } });

    $(document).on('submit', '[data-ajax-list] form', function (e) {
        e.preventDefault();
        AjaxList.load($(this).closest('[data-ajax-list]'), 1);
    });
    $(document).on('change', '[data-ajax-list] form select', function () {
        AjaxList.load($(this).closest('[data-ajax-list]'), 1);
    });
    $(document).on('click', '.pager-link', function (e) {
        e.preventDefault();
        if ($(this).parent().hasClass('disabled') || $(this).parent().hasClass('active')) return;
        AjaxList.load($(this).closest('[data-ajax-list]'), $(this).data('page'));
    });

    // ===== CRUD bằng modal =====
    var $crud = $('#crud');
    if (!$crud.length) return;
    var ctrl = $crud.data('controller');
    var modal = new bootstrap.Modal(document.getElementById('crudModal'));
    var checkTimer = null;

    function showErrors(list) { $('#formErrors').removeClass('d-none').html(list.join('<br>')); }

    function openForm(id) {
        $.get('/' + ctrl + '/Form', id ? { id: id } : {}, function (html) {
            $('#crudFields').html(html);
            $('#isNew').val(id ? 'false' : 'true');
            $('#formErrors').addClass('d-none').empty();
            modal.show();
            if ($('#checkInfo').length) runCheck();
        }).fail(function () { showToast('Không mở được biểu mẫu.', false); });
    }

    $('#btnAdd').on('click', function () { openForm(null); });
    $(document).on('click', '.btn-edit', function () { openForm($(this).data('id')); });

    $('#btnSave').on('click', function () {
        $.post('/' + ctrl + '/Save', $('#crudForm').serialize(), function (r) {
            if (r.ok) { modal.hide(); AjaxList.reload($crud); showToast(window.I18N.saved); }
            else showErrors(r.errors);
        }).fail(function () { showErrors(['Có lỗi xảy ra, vui lòng thử lại.']); });
    });

    $(document).on('click', '.btn-del', function () {
        if (!confirm(window.I18N.confirmDelete)) return;
        $.post('/' + ctrl + '/Delete', { id: $(this).data('id') }, function (r) {
            if (r.ok) { AjaxList.reload($crud); showToast(window.I18N.deleted); }
            else showToast(r.errors.join(' '), false);
        });
    });

    // Kiểm tra xung đột lịch ngay khi người dùng thay đổi trường dữ liệu (AJAX)
    function runCheck() {
        var $info = $('#checkInfo'); if (!$info.length) return;
        $.post($info.data('check-url'), $('#crudForm').serialize(), function (r) {
            if (r.ok) $info.removeClass('alert-warning d-none').addClass('alert-success').html('<i class="bi bi-check-circle"></i> Không phát hiện xung đột.');
            else $info.removeClass('alert-success d-none').addClass('alert-warning').html('<i class="bi bi-exclamation-triangle"></i> ' + r.errors.join('<br>'));
        });
    }
    $(document).on('change input', '#crudForm [data-check]', function () {
        clearTimeout(checkTimer); checkTimer = setTimeout(runCheck, 300);
    });
});
