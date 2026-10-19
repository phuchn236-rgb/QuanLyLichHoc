// Thời khóa biểu dạng lưới: nạp dữ liệu từ Web API và dựng bảng 12 tiết x 7 ngày
(function () {
    var $g = $('#tkbGrid'); if (!$g.length) return;
    var endpoint = $g.data('endpoint'), maHK = +$g.data('mahk'), tuan = +$g.data('tuan');
    var DAYS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];
    var TIMES = ['07:00', '07:55', '08:50', '10:00', '10:55', '12:30', '13:25', '14:20', '15:50', '16:45', '18:00', '18:55'];

    function fmt(iso) { var p = iso.split('-'); return p[2] + '/' + p[1] + '/' + p[0]; }
    function color(s) { var h = 0; for (var i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) % 360; return 'hsl(' + h + ',70%,85%)'; }
    function esc(s) { return $('<div>').text(s || '').html(); }

    function build() {
        var h = '<thead><tr><th style="width:84px">Tiết</th>';
        DAYS.forEach(function (d) { h += '<th>' + d + '</th>'; });
        h += '</tr></thead><tbody>';
        for (var t = 1; t <= 12; t++) {
            h += '<tr><th>' + t + '<br><small class="text-muted">' + TIMES[t - 1] + '</small></th>';
            for (var d = 2; d <= 8; d++) h += '<td id="c' + d + '_' + t + '"></td>';
            h += '</tr>';
        }
        $g.html(h + '</tbody>');
    }

    function load() {
        $('#lblTuan').text(tuan);
        $.getJSON(endpoint, { maHK: maHK, tuan: tuan }, function (r) {
            build();
            $('#lblRange').text(fmt(r.tuanTu) + ' – ' + fmt(r.tuanDen));
            r.items.forEach(function (b) {
                var $c = $('#c' + b.thu + '_' + b.tietBatDau);
                if (!$c.length) return; // ô đã bị gộp (dữ liệu chồng lấn bất thường)
                $c.attr('rowspan', b.soTiet).addClass('slot-busy').css('background', color(b.maMH || b.maLHP))
                  .html('<b>' + esc(b.tenMon) + '</b><br>' + esc(b.phong) + '<br><small>' + esc(b.giangVien) + '<br>' + esc(b.thoiGian) + '</small>');
                for (var k = 1; k < b.soTiet; k++) $('#c' + b.thu + '_' + (b.tietBatDau + k)).remove();
            });
        }).fail(function () { build(); showToast('Không tải được thời khóa biểu.', false); });
    }

    $('#btnPrev').on('click', function () { if (tuan > 1) { tuan--; load(); } });
    $('#btnNext').on('click', function () { if (tuan < 20) { tuan++; load(); } });
    $('#selHK').on('change', function () { location.href = location.pathname + '?maHK=' + $(this).val(); });
    build(); load();
})();
